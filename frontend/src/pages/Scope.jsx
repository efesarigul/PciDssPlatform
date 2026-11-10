import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { scopeApi } from "../api";
import { Icon, PageHeader } from "../ui";

export const SAQ_LABEL = {
  A: "A", AEP: "A-EP", B: "B", BIP: "B-IP", C: "C", CVT: "C-VT", P2PE: "P2PE", SPoC: "SPoC",
  DMerchant: "D (Üye İşyeri)", DServiceProvider: "D (Servis Sağlayıcı)",
};

const ECOMMERCE = [
  { value: "FullyOutsourced", label: "Ödeme sayfası tamamen ödeme kuruluşunda (yönlendirme veya iframe)" },
  { value: "PartialOutsourced", label: "Ödeme formu bizim sitemizden geliyor, kart verisi doğrudan ödeme kuruluşuna gidiyor (direct post / JavaScript)" },
  { value: "OwnSystems", label: "Kart verisini kendi sunucumuz alıyor veya işliyor" },
];
const CARD_PRESENT = [
  { value: "StandaloneDialup", label: "Telefon hattı/mobil bağlantılı, başka sisteme bağlı olmayan POS cihazı" },
  { value: "StandaloneIp", label: "İnternet (IP) bağlantılı, başka sisteme bağlı olmayan POS cihazı" },
  { value: "ValidatedP2pe", label: "PCI listesinde yer alan doğrulanmış P2PE çözümü" },
  { value: "SoftwarePinOnCots", label: "PCI listesinde yer alan SPoC çözümü (telefon/tablet üzerinde PIN girişi)" },
  { value: "PaymentAppConnected", label: "İnternete bağlı yazarkasa/POS yazılımı veya ödeme uygulaması" },
];
const MOTO = [
  { value: "FullyOutsourced", label: "Tamamen ödeme kuruluşuna devredilmiş (kart verisine hiç dokunmuyoruz)" },
  { value: "VirtualTerminal", label: "Ödeme kuruluşunun web tabanlı sanal POS ekranına elle giriyoruz" },
  { value: "OwnSystems", label: "Kendi sistemlerimize giriyoruz" },
];

const EMPTY = { entityType: "Merchant", storesCardDataElectronically: false, ecommerce: null, cardPresent: null, moto: null };

function Channel({ title, value, options, onChange }) {
  const enabled = value !== null;
  return (
    <div className="channel">
      <label className="check">
        <input type="checkbox" checked={enabled} onChange={(e) => onChange(e.target.checked ? options[0].value : null)} />
        <strong>{title}</strong>
      </label>
      {enabled && (
        <div className="options">
          {options.map((o) => (
            <label key={o.value} className={`option ${value === o.value ? "selected" : ""}`}>
              <input type="radio" checked={value === o.value} onChange={() => onChange(o.value)} />
              {o.label}
            </label>
          ))}
        </div>
      )}
    </div>
  );
}

export default function Scope() {
  const nav = useNavigate();
  const [form, setForm] = useState(EMPTY);
  const [result, setResult] = useState(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    scopeApi.get().then((r) => { if (r) { setForm(r.scope); setResult(r); } }).catch((e) => setError(e.message));
  }, []);

  const set = (patch) => { setForm({ ...form, ...patch }); setResult(null); };
  const isSp = form.entityType === "ServiceProvider";

  const save = async () => {
    setSaving(true); setError("");
    try { setResult(await scopeApi.save(form)); }
    catch (e) { setError(e.message); } finally { setSaving(false); }
  };

  return (
    <section>
      <PageHeader eyebrow="Adım 1 / 4" title="Kapsam Belirleme"
        subtitle="Kart ödemelerini nasıl kabul ettiğinizi beyan edin. Size uygun SAQ (öz değerlendirme anketi) tipi ve buna göre sorulacak PCI DSS gereksinimleri bu cevaplardan belirlenir." />

      <div className="card">
        <h3>Kurum tipi</h3>
        <div className="options">
          <label className={`option ${!isSp ? "selected" : ""}`}>
            <input type="radio" checked={!isSp} onChange={() => set({ entityType: "Merchant" })} />
            Üye işyeri: kendi mal/hizmetimiz için kartlı ödeme alıyoruz
          </label>
          <label className={`option ${isSp ? "selected" : ""}`}>
            <input type="radio" checked={isSp} onChange={() => set({ entityType: "ServiceProvider" })} />
            Servis sağlayıcı: başka kurumlar adına kart verisini işliyor, saklıyor veya güvenliğini etkiliyoruz
          </label>
        </div>

        {!isSp && (<>
          <h3>Kart verisi saklama</h3>
          <label className="check">
            <input type="checkbox" checked={form.storesCardDataElectronically}
              onChange={(e) => set({ storesCardDataElectronically: e.target.checked })} />
            Kart numaralarını (PAN) veritabanı, dosya, log gibi elektronik ortamlarda saklıyoruz
          </label>

          <h3>Ödeme kanalları</h3>
          <Channel title="E-ticaret (internet üzerinden ödeme)" value={form.ecommerce} options={ECOMMERCE} onChange={(v) => set({ ecommerce: v })} />
          <Channel title="Yüz yüze ödeme (POS cihazı)" value={form.cardPresent} options={CARD_PRESENT} onChange={(v) => set({ cardPresent: v })} />
          <Channel title="Posta/telefon siparişi (MOTO)" value={form.moto} options={MOTO} onChange={(v) => set({ moto: v })} />
        </>)}

        {error && <div className="error">{error}</div>}

        {result && (
          <div className="saq-result">
            <span className="saq-icon"><Icon name="checkCircle" size={22} /></span>
            <div>
              <div className="saq-title">Size uygun anket: <span className="saq-badge">SAQ {SAQ_LABEL[result.saqType]}</span></div>
              <p>{result.explanation}</p>
              <p className="muted">Bu SAQ kapsamında <strong>{result.questionCount} gereksinim</strong> sorulacak. Kesin SAQ tipini edinci bankanız onaylar.</p>
            </div>
          </div>
        )}

        <div className="actions">
          <span />
          {result
            ? <button onClick={() => nav("/survey")}>Beyan Anketine Geç <Icon name="arrowRight" size={16} /></button>
            : <button disabled={saving} onClick={save}>{saving ? "Kaydediliyor…" : "SAQ Tipini Belirle"}</button>}
        </div>
      </div>
    </section>
  );
}
