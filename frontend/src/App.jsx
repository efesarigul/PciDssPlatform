import { NavLink, Navigate, Route, Routes, useNavigate } from "react-router-dom";
import Login from "./pages/Login";
import Scope from "./pages/Scope";
import SurveyWizard from "./pages/SurveyWizard";
import Evidence from "./pages/Evidence";
import Gap from "./pages/Gap";
import { Icon, Logo } from "./ui";

const STEPS = [
  { to: "/scope", icon: "target", label: "Kapsam Belirleme" },
  { to: "/survey", icon: "clipboard", label: "Beyan Anketi" },
  { to: "/evidence", icon: "upload", label: "Kanıt Yükleme" },
  { to: "/gap", icon: "chart", label: "Gap Analizi" },
];

function Protected({ children }) {
  return localStorage.getItem("token") ? children : <Navigate to="/login" replace />;
}

function Shell({ children }) {
  const nav = useNavigate();
  const org = localStorage.getItem("org") ?? "";
  const initials = org.split(/\s+/).filter((w) => /^[\p{L}]/u.test(w)).slice(0, 2).map((w) => w[0]).join("").toUpperCase();
  const logout = () => { localStorage.clear(); nav("/login"); };
  return (
    <div className="shell">
      <aside className="sidebar">
        <Logo light />
        <div className="nav-label">Değerlendirme adımları</div>
        <nav>
          {STEPS.map((s, i) => (
            <NavLink key={s.to} to={s.to}>
              <Icon name={s.icon} />
              <span>{s.label}</span>
              <span className="nav-step">{i + 1}</span>
            </NavLink>
          ))}
        </nav>
        <div className="org-card">
          <span className="avatar">{initials || "K"}</span>
          <span className="org-meta"><strong title={org}>{org}</strong><span>Kurum hesabı</span></span>
          <button className="icon-btn" onClick={logout} title="Çıkış yap" aria-label="Çıkış yap"><Icon name="logout" /></button>
        </div>
      </aside>
      <main className="content">{children}</main>
    </div>
  );
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route path="/scope" element={<Protected><Shell><Scope /></Shell></Protected>} />
      <Route path="/survey"element={<Protected><Shell><SurveyWizard /></Shell></Protected>} />
      <Route path="/evidence" element={<Protected><Shell><Evidence /></Shell></Protected>} />
      <Route path="/gap" element={<Protected><Shell><Gap /></Shell></Protected>} />
      <Route path="*" element={<Navigate to="/survey" replace />} />
    </Routes>
  );
}
