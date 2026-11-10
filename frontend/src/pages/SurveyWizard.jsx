import { useEffect, useRef, useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { surveyApi } from "../api";
import { SAQ_LABEL } from "./Scope";
import { Icon, PageHeader } from "../ui";
import { presetsFor } from "../presets";

// SAQ cevap seçenekleri
const OPTIONS = [
  { value: "InPlace", label: "Uygulanıyor" },
  { value: "InPlaceWithCcw", label: "Telafi edici kontrolle uygulanıyor (CCW)", justify: "Uygulanan telafi edici kontrolü ve gereksinimi neden doğrudan karşılayamadığınızı açıklayın." },
  { value: "NotApplicable", label: "Uygulanamaz", justify: "Gereksinimin ortamınızda neden geçerli olmadığını açıklayın." },
  { value: "NotTested", label: "Test edilmedi", justify: "Bu gereksinimin neden değerlendirme dışında bırakıldığını açıklayın." },
  { value: "NotInPlace", label: "Uygulanmıyor" },
];

export const SECTIONS = {
  1: "Ağ güvenlik kontrolleri", 2: "Güvenli yapılandırma", 3: "Saklanan hesap verisinin korunması",
  4: "İletimde kriptografi", 5: "Kötü amaçlı yazılımdan korunma", 6: "Güvenli sistem ve yazılım",
  7: "Erişimin kısıtlanması", 8: "Kimlik doğrulama", 9: "Fiziksel erişim", 10: "Kayıt ve izleme",
  11: "Güvenlik testleri", 12: "Politika ve programlar",
};

export default function SurveyWizard() {
  const nav = useNavigate();
  // Gap analizindeki bir bulgudan ?q=<soruId> ile gelindiyse o soru açılır, kaydedince panele dönülür
  const [params] = useSearchParams();
  const fromGap = params.get("q");
  const [saq, setSaq] = useState(null);
  const [questions, setQuestions] = useState([]);
  const [answers, setAnswers] = useState({});
  const [step, setStep] = useState(0);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [needsScope, setNeedsScope] = useState(false);
  const commentRef = useRef(null);

  useEffect(() => {
    surveyApi.questions().then(({ saqType, questions: qs }) => {
      setSaq(saqType);
      setQuestions(qs);
      const init = {};
      qs.forEach((q) => { if (q.currentAnswer) init[q.id] = { value: q.currentAnswer, comment: q.currentComment ?? "" }; });
      setAnswers(init);
      const target = qs.findIndex((q) => String(q.id) === fromGap);
      const firstEmpty = qs.findIndex((q) => !q.currentAnswer);
      setStep(target !== -1 ? target : firstEmpty === -1 ? 0 : firstEmpty);
    }).catch((e) => { setError(e.message); setNeedsScope(e.message.includes("Kapsam")); });
  }, []);

  if (needsScope) return (
    <section>
      <PageHeader eyebrow="Adım 2 / 4" title="Beyan Anketi" />
      <div className="card">
        <p>Size sorulacak PCI DSS gereksinimleri SAQ tipinize göre belirlenir.</p>
        <Link to="/scope"><button>Kapsam Belirlemeye Git <Icon name="arrowRight" size={16} /></button></Link>
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
  // ":" ile biten hazır gerekçe tamamlanmadan kaydedilemez
  const missingJustification = selected?.justify && (!a.comment.trim() || a.comment.trim().endsWith(":"));
  const presets = presetsFor(q.requirementCode, a.value);
  // Hazır gerekçe alana yazılır; ":" ile bitenler tamamlansın diye imleç sona konur
  const applyPreset = (text) => {
    update({ comment: text.endsWith(":") ? `${text} ` : text });
    requestAnimationFrame(() => {
      const el = commentRef.current;
      if (el) { el.focus(); el.setSelectionRange(el.value.length, el.value.length); }
    });
  };

  // Bölüm adımları: her bölümün ilk sorusuna atlar, tüm soruları cevaplanınca "done" olur
  const sections = [...new Set(questions.map((x) => x.section))];
  const sectionDone = (s) => questions.filter((x) => x.section === s).every((x) => answers[x.id]);
  const inSection = questions.filter((x) => x.section === q.section);

  // Her adımda kaydet -> kullanıcı yarıda bırakırsa veri kaybolmaz
  const next = async () => {
    setSaving(true); setError("");
    try {
      await surveyApi.save([{ questionId: q.id, value: a.value, comment: a.comment }]);
      if (fromGap) nav("/gap");
      else isLast ? nav("/evidence") : setStep(step + 1);
    } catch (e) { setError(e.message); } finally { setSaving(false); }
  };

  return (
    <section>
      <PageHeader eyebrow="Adım 2 / 4" title="Beyan Anketi"
        subtitle={`SAQ ${SAQ_LABEL[saq]} · ${questions.length} gereksinim. Her cevap anında kaydedilir; yarıda bırakıp sonra devam edebilirsiniz.`} />
      <div className="card survey-progress">
        <div className="survey-progress-head">
          <span><strong>{Object.keys(answers).length}</strong> / {questions.length} cevaplandı</span>
          <span className="muted">Bölüm {q.section} · {SECTIONS[q.section]}</span>
        </div>
        <div className="progress"><div style={{ width: `${(Object.keys(answers).length / questions.length) * 100}%` }} /></div>
        <div className="stepper">
          {sections.map((s) => (
            <button key={s} onClick={() => setStep(questions.findIndex((x) => x.section === s))}
              className={`dot ${s === q.section ? "active" : ""} ${sectionDone(s) ? "done" : ""}`}
              title={SECTIONS[s]}>{sectionDone(s) && s !== q.section ? <Icon name="check" size={14} /> : s}</button>
          ))}
        </div>
      </div>

      <div className="card question-card">
        <div className="question-meta">
          <span className="req-tag">{q.requirementCode} · {q.requirementTitle}</span>
          <span className="muted">Soru {inSection.indexOf(q) + 1}/{inSection.length} · toplam {step + 1}/{questions.length}</span>
        </div>
        <h3>{q.text}</h3>
        <p className="muted">{q.requirementDescription}</p>
        <div className="options">
          {OPTIONS.map((o) => (
            <label key={o.value} className={`option answer-${o.value} ${a.value === o.value ? "selected" : ""}`}>
              <input type="radio" name={`q${q.id}`} checked={a.value === o.value} onChange={() => update({ value: o.value })} />
              {o.label}
            </label>
          ))}
        </div>
        {presets.length > 0 && (
          <div className="presets">
            <span className="presets-label">Hazır gerekçeler · tıklayın, isterseniz düzenleyin</span>
            {presets.map((p) => (
              <button key={p.text} type="button" onClick={() => applyPreset(p.text)}
                className={`preset ${p.specific ? "specific" : ""} ${a.comment === p.text ? "on" : ""}`}>
                {p.specific && <span className="preset-tag">Bu gereksinime özel</span>}
                {p.text}
              </button>
            ))}
          </div>
        )}
        <label>{selected?.justify ? "Gerekçe (zorunlu)" : "Açıklama (opsiyonel)"}
          <textarea ref={commentRef} rows={3} value={a.comment} onChange={(e) => update({ comment: e.target.value })}
            placeholder={selected?.justify ?? "Mevcut kontrolü kısaca açıklayın…"} />
        </label>
        {q.evidenceRequired && (a.value === "InPlace" || a.value === "InPlaceWithCcw") && (
          <div className={`hint ${q.evidenceUploaded ? "ok" : ""}`}>
            {q.evidenceUploaded
              ? <><Icon name="checkCircle" size={16} /> Bu gereksinim için kanıt yüklenmiş.</>
              : <><Icon name="file" size={16} /><span>Beklenen kanıt: <strong>{q.evidenceHint}</strong>. Kanıt yüklenmezse gap analizinde eksik olarak işaretlenir.</span></>}
          </div>
        )}
        {error && <div className="error">{error}</div>}
        <div className="actions">
          {fromGap
            ? <button className="ghost" onClick={() => nav("/gap")}><Icon name="arrowLeft" size={16} /> Gap Analizine Dön</button>
            : <button className="ghost" disabled={step === 0} onClick={() => setStep(step - 1)}><Icon name="arrowLeft" size={16} /> Geri</button>}
          <button disabled={!a.value || missingJustification || saving} onClick={next}>
            {saving ? "Kaydediliyor…" : fromGap ? "Kaydet, Gap Analizine Dön" : isLast ? "Bitir, Kanıt Yükle" : "Kaydet ve İlerle"}
            {!saving && <Icon name="arrowRight" size={16} />}
          </button>
        </div>
      </div>
    </section>
  );
}
