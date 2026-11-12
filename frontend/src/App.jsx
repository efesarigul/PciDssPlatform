import { NavLink, Navigate, Route, Routes, useNavigate } from "react-router-dom";
import Login from "./pages/Login";
import Scope from "./pages/Scope";
import SurveyWizard from "./pages/SurveyWizard";
import Evidence from "./pages/Evidence";
import Gap from "./pages/Gap";
import Auditors from "./pages/Auditors";
import { AuditOrgs, AuditReview } from "./pages/Audit";
import { Icon, Logo } from "./ui";

const STEPS = [
  { to: "/scope", icon: "target", label: "Kapsam Belirleme" },
  { to: "/survey", icon: "clipboard", label: "Beyan Anketi" },
  { to: "/evidence", icon: "upload", label: "Kanıt Yükleme" },
  { to: "/gap", icon: "chart", label: "Gap Analizi" },
];

export const isAuditor = () => localStorage.getItem("role") === "Auditor";
const home = () => (isAuditor() ? "/audit" : "/survey");

// role: "auditor" yalnızca denetçiye, "customer" yalnızca kurum hesabına açık sayfalar
function Protected({ role, children }) {
  if (!localStorage.getItem("token")) return <Navigate to="/login" replace />;
  if ((role === "auditor") !== isAuditor()) return <Navigate to={home()} replace />;
  return children;
}

function NavItem({ to, icon, label, step }) {
  return (
    <NavLink to={to}>
      <Icon name={icon} />
      <span>{label}</span>
      {step && <span className="nav-step">{step}</span>}
    </NavLink>
  );
}

function Shell({ children }) {
  const nav = useNavigate();
  const auditor = isAuditor();
  const org = localStorage.getItem("org") ?? "";
  const initials = org.split(/\s+/).filter((w) => /^[\p{L}]/u.test(w)).slice(0, 2).map((w) => w[0]).join("").toUpperCase();
  const logout = () => { localStorage.clear(); nav("/login"); };
  return (
    <div className="shell">
      <aside className="sidebar">
        <Logo light />
        {auditor ? (
          <>
            <div className="nav-label">Denetçi paneli</div>
            <nav><NavItem to="/audit" icon="building" label="Kurumlar" /></nav>
          </>
        ) : (
          <>
            <div className="nav-label">Değerlendirme adımları</div>
            <nav>
              {STEPS.map((s, i) => <NavItem key={s.to} {...s} step={i + 1} />)}
              <div className="nav-label">Paylaşım</div>
              <NavItem to="/auditors" icon="users" label="Denetçi Erişimi" />
            </nav>
          </>
        )}
        <div className="org-card">
          <span className="avatar">{initials || "K"}</span>
          <span className="org-meta"><strong title={org}>{org}</strong><span>{auditor ? "Denetçi hesabı" : "Kurum hesabı"}</span></span>
          <button className="icon-btn" onClick={logout} title="Çıkış yap" aria-label="Çıkış yap"><Icon name="logout" /></button>
        </div>
      </aside>
      <main className="content">{children}</main>
    </div>
  );
}

const page = (el, role = "customer") => <Protected role={role}><Shell>{el}</Shell></Protected>;

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route path="/scope" element={page(<Scope />)} />
      <Route path="/survey" element={page(<SurveyWizard />)} />
      <Route path="/evidence" element={page(<Evidence />)} />
      <Route path="/gap" element={page(<Gap />)} />
      <Route path="/auditors" element={page(<Auditors />)} />
      <Route path="/audit" element={page(<AuditOrgs />, "auditor")} />
      <Route path="/audit/:orgId" element={page(<AuditReview />, "auditor")} />
      <Route path="*" element={<Navigate to={localStorage.getItem("token") ? home() : "/login"} replace />} />
    </Routes>
  );
}
