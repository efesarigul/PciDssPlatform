import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { surveyApi } from "../api";
import { SAQ_LABEL } from "./Scope";

// SAQ cevap seçenekleri
const OPTIONS = [
  { value: "InPlace", label: "Uygulanıyor" },
  { value: "InPlaceWithCcw", label: "Telafi edici kontrolle uygulanıyor (CCW)", justify: "Uygulanan telafi edici kontrolü ve gereksinimi neden doğrudan karşılayamadığınızı açıklayın." },
  { value: "NotApplicable", label: "Uygulanamaz", justify: "Gereksinimin ortamınızda neden geçerli olmadığını açıklayın." },
  { value: "NotTested", label: "Test edilmedi", justify: "Bu gereksinimin neden değerlendirme dışında bırakıldığını açıklayın." },
  { value: "NotInPlace", label: "Uygulanmıyor" },
];

const SECTIONS = {
  1: "Ağ güvenlik kontrolleri", 2: "Güvenli yapılandırma", 3: "Saklanan hesap verisinin korunması",
  4: "İletimde kriptografi", 5: "Kötü amaçlı yazılımdan korunma", 6: "Güvenli sistem ve yazılım",
  7: "Erişimin kısıtlanması", 8: "Kimlik doğrulama", 9: "Fiziksel erişim", 10: "Kayıt ve izleme",
  11: "Güvenlik testleri", 12: "Politika ve programlar",
};

export default function SurveyWizard() {
  const nav = useNavigate();
  const [saq, setSaq] = useState(null);
  const [questions, setQuestions] = useState([]);
  const [answers, setAnswers] = useState({});
  const [step, setStep] = useState(0);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [needsScope, setNeedsScope] = useState(false);

  useEffect(() => {
    surveyApi.questions().then(({ saqType, questions: qs }) => {
      setSaq(saqType);
      setQuestions(qs);
      const init = {};
      qs.forEach((q) => { if (q.currentAnswer) init[q.id] = { value: q.currentAnswer, comment: q.currentComment ?? "" }; });
      setAnswers(init);
      const firstEmpty = qs.findIndex((q) => !q.currentAnswer);
      setStep(firstEmpty === -1 ? 0 : firstEmpty);
    }).catch((e) => { setError(e.message); setNeedsScope(e.message.includes("Kapsam")); });
  }, []);

  if (needsScope) return (
    <section>
      <h2>Beyan Anketi</h2>
      <div className="card">
        <p>Size sorulacak PCI DSS gereksinimleri SAQ tipinize göre belirlenir.</p>
        <Link to="/scope"><button>Kapsam Belirlemeye Git →</button></Link>
      </div>
    </section>
  );
  if (error && !questions.length) return <div className="error">{error}</div>;
  if (!questions.length) return <p className="muted">Yükleniyor…</p>;

  const q = questions[step];
  const a = answers[q.id] ?? { value: null, comment: "" };
  const update = (patch) => setAnswers({ ...answers, [q.id]: { ...a, ...patch } });
  const isLast = step === questions.length - 1;
  const selected = OPTIONS.find((o) => o.value === a.value);
  const missingJustification = selected?.justify && !a.comment.trim();

  // Bölüm adımları: her bölümün ilk sorusuna atlar, tüm soruları cevaplanınca "done" olur
  const sections = [...new Set(questions.map((x) => x.section))];
  const sectionDone = (s) => questions.filter((x) => x.section === s).every((x) => answers[x.id]);
  const inSection = questions.filter((x) => x.section === q.section);

  // Her adımda kaydet -> kullanıcı yarıda bırakırsa veri kaybolmaz
  const next = async () => {
    setSaving(true); setError("");
    try {
      await surveyApi.save([{ questionId: q.id, value: a.value, comment: a.comment }]);
      isLast ? nav("/evidence") : setStep(step + 1);
    } catch (e) { setError(e.message); } finally { setSaving(false); }
  };

  return (
    <section>
      <h2>Beyan Anketi <span className="muted">· SAQ {SAQ_LABEL[saq]}</span></h2>
      <div className="stepper">
        {sections.map((s) => (
          <button key={s} onClick={() => setStep(questions.findIndex((x) => x.section === s))}
            className={`dot ${s === q.section ? "active" : ""} ${sectionDone(s) ? "done" : ""}`}
            title={SECTIONS[s]}>{s}</button>
        ))}
      </div>
      <div className="progress"><div style={{ width: `${(Object.keys(answers).length / questions.length) * 100}%` }} /></div>

      <div className="card">
        <div className="muted">Gereksinim {q.section} · {SECTIONS[q.section]} — soru {inSection.indexOf(q) + 1}/{inSection.length}
          {" "}(toplam {step + 1}/{questions.length})</div>
        <div className="req-tag">{q.requirementCode} · {q.requirementTitle}</div>
        <h3>{q.text}</h3>
        <p className="muted">{q.requirementDescription}</p>
        <div className="options">
          {OPTIONS.map((o) => (
            <label key={o.value} className={`option ${a.value === o.value ? "selected" : ""}`}>
              <input type="radio" name={`q${q.id}`} checked={a.value === o.value} onChange={() => update({ value: o.value })} />
              {o.label}
            </label>
          ))}
        </div>
        <label>{selected?.justify ? "Gerekçe (zorunlu)" : "Açıklama (opsiyonel)"}
          <textarea rows={3} value={a.comment} onChange={(e) => update({ comment: e.target.value })}
            placeholder={selected?.justify ?? "Mevcut kontrolü kısaca açıklayın…"} />
        </label>
        {q.evidenceRequired && (a.value === "InPlace" || a.value === "InPlaceWithCcw") && (
          <div className="hint">
            {q.evidenceUploaded
              ? <>✅ Bu gereksinim için kanıt yüklenmiş.</>
              : <>📎 Beklenen kanıt: <strong>{q.evidenceHint}</strong>. Kanıt yüklenmezse gap analizinde eksik olarak işaretlenir.</>}
          </div>
        )}
        {error && <div className="error">{error}</div>}
        <div className="actions">
          <button className="ghost" disabled={step === 0} onClick={() => setStep(step - 1)}>← Geri</button>
          <button disabled={!a.value || missingJustification || saving} onClick={next}>
            {saving ? "Kaydediliyor…" : isLast ? "Bitir → Kanıt Yükle" : "Kaydet ve İlerle →"}
          </button>
        </div>
      </div>
    </section>
  );
}
