import { useEffect, useState } from "react";
import { auditorsApi } from "../api";
import { Icon, PageHeader } from "../ui";

const fmtDate = (d) => new Date(d).toLocaleDateString("tr-TR", { dateStyle: "medium" });

// Kurumun, beyan ve kanıtlarını inceleyebilecek denetçileri yönettiği ekran
export default function Auditors() {
  const [list, setList] = useState(null);
  const [email, setEmail] = useState("");
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);

  useEffect(() => { auditorsApi.list().then(setList).catch((e) => setError(e.message)); }, []);

  const grant = async (e) => {
    e.preventDefault();
    setError(""); setBusy(true);
    try {
      const a = await auditorsApi.grant(email);
      setList((l) => [...(l ?? []), a]);
      setEmail("");
    } catch (err) { setError(err.message); } finally { setBusy(false); }
  };

  const revoke = async (a) => {
    if (!window.confirm(`${a.fullName} (${a.firmName}) artık beyan ve kanıtlarınızı göremeyecek. Devam edilsin mi?`)) return;
    try {
      await auditorsApi.revoke(a.id);
      setList((l) => l.filter((x) => x.id !== a.id));
    } catch (err) { setError(err.message); }
  };

  return (
    <section>
      <PageHeader eyebrow="Paylaşım" title="Denetçi Erişimi"
        subtitle="Erişim verdiğiniz denetçiler, kanıt yüklediğiniz beyanlarınızı inceleyip onaylar veya reddeder. Denetçiler verinizi değiştiremez; kararları Gap Analizi ekranınıza yansır." />

      <form className="card grant-card" onSubmit={grant}>
        <label>Denetçinin e-posta adresi
          <input type="email" required value={email} onChange={(e) => setEmail(e.target.value)} placeholder="denetci@denetimfirmasi.com" />
        </label>
        <button disabled={busy}><Icon name="users" size={16} /> Erişim ver</button>
      </form>
      <p className="muted small grant-note">Denetçinin platformda denetçi hesabı açmış olması gerekir. Erişimi istediğiniz zaman kaldırabilirsiniz.</p>
      {error && <div className="error">{error}</div>}

      <div className="card table-card">
        <div className="table-head"><h2>Erişimi olan denetçiler</h2><div className="table-stats"><span>{list?.length ?? 0} denetçi</span></div></div>
        <table className="table">
          <thead><tr><th>Denetçi</th><th>Denetim firması</th><th>E-posta</th><th>Erişim tarihi</th><th className="keep" /></tr></thead>
          <tbody>
            {list === null && <tr><td colSpan={5} className="muted center">Yükleniyor…</td></tr>}
            {list?.length === 0 && <tr><td colSpan={5} className="muted center">Henüz hiçbir denetçiye erişim vermediniz.</td></tr>}
            {list?.map((a) => (
              <tr key={a.id}>
                <td><strong>{a.fullName}</strong></td>
                <td>{a.firmName}</td>
                <td className="muted">{a.email}</td>
                <td className="muted nowrap">{fmtDate(a.grantedAt)}</td>
                <td className="right keep"><button className="ghost sm" onClick={() => revoke(a)}>Erişimi kaldır</button></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}
