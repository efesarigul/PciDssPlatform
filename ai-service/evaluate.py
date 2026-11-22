"""
Değerlendirme: eval/cases.json'daki etiketli belgeleri /analyze uç noktasından geçirir, madde bazında doğruluk ölçer.
Farklı modları ve modelleri karşılaştırmak için:

  set ANALYSIS_MODE=rules && python evaluate.py
  set LLM_MODEL=qwen3:4b  && python evaluate.py
  set LLM_MODEL=gemma3:4b && python evaluate.py
"""
import json
import sys
import time
from pathlib import Path

from fastapi.testclient import TestClient

BASE = Path(__file__).parent
sys.stdout.reconfigure(encoding="utf-8")  # Windows konsolunda ✓ ✗ ve Türkçe karakterler
sys.path.insert(0, str(BASE))
import app as A  # noqa: E402

cases = json.loads((BASE / "eval" / "cases.json").read_text(encoding="utf-8"))["cases"]
client = TestClient(A.app)
total = correct = 0
start = time.time()
rows = []
for n, case in enumerate(cases):
    files, ids = [], []
    for i, rel in enumerate(case["docs"]):
        p = (BASE / rel).resolve()
        files.append(("files", (p.name, p.read_bytes(), "text/plain")))
        ids.append(90000 + n * 10 + i)
    r = client.post("/analyze", files=files, data={
        "policy_ids": ids, "target_policy_id": ids[0], "org_id": 0, "requirement_code": case["code"],
        "requirement_title": case["title"], "requirement_description": case["description"]})
    if r.status_code != 200:
        print(f"{case['code']}: HTTP {r.status_code} {r.text[:200]}")
        continue
    for ch in r.json()["checks"]:
        ok_values = case["expected"].get(ch["id"])
        if not ok_values:
            continue
        ok = ch["status"] in ok_values
        total += 1
        correct += ok
        rows.append((ok, f"{case['code']} {ch['id']}", ch["status"], "/".join(ok_values), ch["finding"][:150]))

for ok, key, got, want, finding in rows:
    print(f"{'✓' if ok else '✗'} {key:9} çıkan={got:8} beklenen={want:15} {finding}")
mode = A.MODE if A.MODE == "rules" else f"hybrid/{A.CHAT_MODEL}"
print(f"\n[{mode}] doğru: {correct}/{total}  süre: {time.time() - start:.0f} sn")
