#!/usr/bin/env python3
"""
Whisper -> NLLB -> választható célnyelv -> fordítás TXT-be -> TTS WAV
Javított verzió, ahol most a felhasználó kiválaszthatja a célnyelvet menüből.
"""

import os
import sys
from pathlib import Path
import subprocess
from typing import List
from pydub import AudioSegment
import whisper
from transformers import AutoTokenizer, AutoModelForSeq2SeqLM
import torch

# Elérhető nyelvek listája
LANGS = {
    1: ("English", "eng_Latn"),
    2: ("German", "deu_Latn"),
    3: ("Spanish", "spa_Latn"),
    4: ("French", "fra_Latn"),
    5: ("Hungarian", "hun_Latn"),
    6: ("Italian", "ita_Latn"),
    7: ("Portuguese", "por_Latn"),
    8: ("Russian", "rus_Cyrl"),
    9: ("Polish", "pol_Latn"),
    10: ("Dutch", "nld_Latn"),
    11: ("Swedish", "swe_Latn"),
    12: ("Turkish", "tur_Latn"),
    13: ("Chinese", "cmn_Hans"),
    14: ("Japanese", "jpn_Jpan"),
    15: ("Czech", "ces_Latn"),
    16: ("Greek", "ell_Grek"),
    17: ("Finnish", "fin_Latn"),
    18: ("Ukrainian", "ukr_Cyrl"),
    19: ("Arabic", "ara_Arab")
}

# Espeak nyelvkódok
ESPEAK = {
    "eng_Latn": "en", "deu_Latn": "de", "spa_Latn": "es", "fra_Latn": "fr",
    "hun_Latn": "hu", "ita_Latn": "it", "por_Latn": "pt", "rus_Cyrl": "ru",
    "pol_Latn": "pl", "nld_Latn": "nl", "swe_Latn": "sv", "tur_Latn": "tr",
    "cmn_Hans": "zh", "jpn_Jpan": "ja", "ces_Latn": "cs", "ell_Grek": "el",
    "fin_Latn": "fi", "ukr_Cyrl": "uk", "ara_Arab": "ar"
}

CHUNK_SEC = 60
NLLB_MODEL = "facebook/nllb-200-distilled-600M"

# Segédfüggvények
def ensure_dir(path: Path):
    path.mkdir(parents=True, exist_ok=True)

def split_audio(input_file: str, chunk_length_s: int, temp_dir: Path) -> List[Path]:
    audio = AudioSegment.from_file(input_file)
    duration_ms = len(audio)
    chunk_ms = chunk_length_s * 1000
    parts = []
    idx = 0
    for start_ms in range(0, duration_ms, chunk_ms):
        end_ms = min(start_ms + chunk_ms, duration_ms)
        piece = audio[start_ms:end_ms]
        out_path = temp_dir / f"chunk_{idx:04d}.wav"
        piece.export(out_path, format="wav")
        parts.append(out_path)
        idx += 1
    return parts

def transcribe_chunks_whisper(model, chunks: List[Path]) -> str:
    texts = []
    for i, chunk in enumerate(chunks):
        print(f"Transcribing chunk {i+1}/{len(chunks)}: {chunk.name}")
        result = model.transcribe(str(chunk))
        text = result.get("text", "").strip()
        texts.append(text)
    return "\n".join(texts)

class NLLBTranslator:
    def __init__(self, model_name=NLLB_MODEL):
        print("Loading NLLB model…")
        self.tokenizer = AutoTokenizer.from_pretrained(model_name)
        self.tokenizer.src_lang = "eng_Latn"
        self.model = AutoModelForSeq2SeqLM.from_pretrained(model_name)
        self.device = "cuda" if torch.cuda.is_available() else "cpu"
        self.model.to(self.device)

    def translate(self, text: str, tgt_lang: str) -> str:
        print(f"Translating to {tgt_lang}")
        inputs = self.tokenizer(text, return_tensors="pt", padding=True, truncation=True).to(self.device)
        forced_id = self.tokenizer.convert_tokens_to_ids(tgt_lang)
        if forced_id is None:
            raise ValueError(f"Target language {tgt_lang} not recognized by tokenizer")
        generated = self.model.generate(**inputs, forced_bos_token_id=forced_id, max_new_tokens=512)
        return self.tokenizer.batch_decode(generated, skip_special_tokens=True)[0]

def espeak_tts(text: str, espeak_lang: str, out_wav: Path):
    print(f"Generating TTS → {out_wav}")
    proc = subprocess.run(["espeak-ng", "-v", espeak_lang, "--stdout", text], stdout=subprocess.PIPE)
    out_wav.write_bytes(proc.stdout)

# Main pipeline
def main():
    print("\n=== ELÉRHETŐ NYELVEK ===")
    for n, (name, code) in LANGS.items():
        print(f"{n}. {name} — {code}")

    choice = int(input("\nVálaszd ki a célnyelvet (szám): "))
    lang_name, tgt_lang = LANGS[choice]
    print(f"\nKiválasztva: {lang_name} ({tgt_lang})\n")

    input_path = input("Add meg a bemeneti hangfájlt: ")
    input_file = Path(input_path)

    outdir = Path("output")
    temp_dir = outdir / "temp"
    trans_dir = outdir / "translations"
    speech_dir = outdir / "speech"
    for d in [outdir, temp_dir, trans_dir, speech_dir]:
        ensure_dir(d)

    print("\nSplitting audio…")
    chunks = split_audio(str(input_file), CHUNK_SEC, temp_dir)
    print(f"Created {len(chunks)} chunks.")

    print("Loading Whisper model…")
    wmodel = whisper.load_model("small")

    print("Transcribing…")
    transcript = transcribe_chunks_whisper(wmodel, chunks)
    original_txt = outdir / "original.txt"
    original_txt.write_text(transcript, encoding="utf-8")
    print(f"Saved original → {original_txt}")

    translator = NLLBTranslator()
    translated = translator.translate(transcript, tgt_lang)

    fp = trans_dir / f"translation_{tgt_lang.replace('_','-')}.txt"
    fp.write_text(translated, encoding="utf-8")
    print(f"Saved translation → {fp}")

    if tgt_lang in ESPEAK:
        wav = speech_dir / f"speech_{tgt_lang.replace('_','-')}.wav"
        espeak_tts(translated, ESPEAK[tgt_lang], wav)
        print(f"Saved speech → {wav}")
    else:
        print(f"Nincs espeak támogatás ehhez a nyelvhez: {tgt_lang}")

    print("\n=== KÉSZ ===")
    print(f"Összes kimenet az 'output' mappában.")

if __name__ == "__main__":
    main()
