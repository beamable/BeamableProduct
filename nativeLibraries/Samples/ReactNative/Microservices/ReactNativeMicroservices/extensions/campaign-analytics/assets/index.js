(function(react, client, jsxRuntime) {
  "use strict";
  function e(e2) {
    let t2 = { mount: (t3, n) => e2.onMount(t3, n), unmount: (t3) => e2.onUnmount(t3), ...e2.getBadge ? { getBadge: e2.getBadge } : {} };
    window[e2.beamId] = t2;
  }
  const t = { registerExtension: e };
  function l(e2) {
    let t2 = ((typeof globalThis < `u` ? globalThis.__beamPortal?.react : void 0) ?? {})[e2];
    if (!t2) throw Error(`Beam React component "${e2}" is not provided by the host portal. Extensions must run inside the Beamable portal — see https://help.beamable.com/ for the extension setup guide.`);
    return t2;
  }
  function d(e2) {
    return react.createElement(l(`BeamSpinner`), e2);
  }
  d.displayName = `BeamSpinner`;
  function p(e2) {
    return react.createElement(l(`BeamButton`), e2);
  }
  p.displayName = `BeamButton`;
  function g(e2) {
    return react.createElement(l(`BeamBadge`), e2);
  }
  g.displayName = `BeamBadge`;
  function y(e2) {
    return react.createElement(l(`BeamCallout`), e2);
  }
  y.displayName = `BeamCallout`;
  function b(e2) {
    return react.createElement(l(`BeamCard`), e2);
  }
  b.displayName = `BeamCard`;
  function w(e2) {
    return react.createElement(l(`BeamDetails`), e2);
  }
  w.displayName = `BeamDetails`;
  function I(e2) {
    return react.createElement(l(`BeamOption`), e2);
  }
  I.displayName = `BeamOption`;
  function ne(e2) {
    return react.createElement(l(`BeamPageHeader`), e2);
  }
  ne.displayName = `BeamPageHeader`;
  function _e(e2) {
    return react.createElement(l(`BeamSelect`), e2);
  }
  _e.displayName = `BeamSelect`;
  function Se(e2) {
    return null;
  }
  Se.displayName = `BeamColumn`;
  function Te(e2) {
    return react.createElement(l(`BeamTable`), e2);
  }
  Te.displayName = `BeamTable`;
  function ke(e2) {
    let [t2, n] = react.useState(null);
    return react.useEffect(() => {
      let t3 = false;
      return e2.beam.then((e3) => {
        t3 || n(e3);
      }), () => {
        t3 = true;
      };
    }, [e2.beam]), t2;
  }
  function Ae(r) {
    let { beamId: i, App: a, disableStrictMode: o, wrapper: s, getBadge: l2 } = r;
    t.registerExtension({ beamId: i, onMount: (e2, r2) => {
      let i2 = client.createRoot(e2), l3 = react.createElement(a, { context: r2 }), u = s ? s({ context: r2, children: l3 }) : l3;
      return i2.render(o ? u : react.createElement(react.StrictMode, null, u)), i2;
    }, onUnmount: (e2) => {
      e2.unmount();
    }, ...l2 ? { getBadge: l2 } : {} });
  }
  const name = "campaign-analytics";
  const STAGES = ["Sent", "Received", "Opened", "Clicked", "Converted"];
  function parseCsvLine(line) {
    const out = [];
    let cur = "";
    let q = false;
    for (let i = 0; i < line.length; i++) {
      const ch = line[i];
      if (ch === '"') {
        if (q && line[i + 1] === '"') {
          cur += '"';
          i++;
        } else q = !q;
      } else if (ch === "," && !q) {
        out.push(cur.trim());
        cur = "";
      } else cur += ch;
    }
    out.push(cur.trim());
    return out;
  }
  function parseCsv(text) {
    const lines = text.trim().split("\n");
    if (lines.length < 2) return [];
    const headers = parseCsvLine(lines[0]);
    const rows = [];
    for (let i = 1; i < lines.length; i++) {
      const vals = parseCsvLine(lines[i]);
      if (vals.length !== headers.length) continue;
      const row = {};
      headers.forEach((h, idx) => row[h] = vals[idx]);
      rows.push(row);
    }
    return rows;
  }
  async function runQuery(beam, sql) {
    const resp = await beam.requester.request({
      url: "/basic/history/query/url",
      method: "POST",
      body: { query: sql },
      headers: { "X-DE-TIMEOUT": "60000" },
      withAuth: true
    });
    const url = resp.body?.url;
    if (!url) return [];
    const r = await fetch(url);
    if (!r.ok) throw new Error(`Result fetch failed (${r.status})`);
    return parseCsv(await r.text());
  }
  async function listFunnelTables(beam) {
    try {
      const resp = await beam.requester.request({
        url: "/basic/history/events",
        method: "GET",
        withAuth: true
      });
      return (resp.body?.namespaces ?? []).filter((n) => /notification_funnel/i.test(n));
    } catch {
      return [];
    }
  }
  const sqlStr = (v) => `'${v.replace(/'/g, "''")}'`;
  const dash = (v) => v && v.length ? v : "—";
  function pickCol(r, name2) {
    const target = name2.toLowerCase().replace(/[^a-z0-9]/g, "");
    for (const k of Object.keys(r)) {
      const norm = k.toLowerCase().replace(/[^a-z0-9]/g, "");
      if (norm === target || norm === "e" + target) {
        const v = r[k];
        if (v != null && v !== "") return v;
      }
    }
    return "";
  }
  function OffersCell({ offers }) {
    if (offers.length === 0) return /* @__PURE__ */ jsxRuntime.jsx("span", { style: { color: "var(--color-beam-text-muted)" }, children: "—" });
    const summary = offers.length === 1 ? "1 offer" : `${offers.length} offers`;
    return /* @__PURE__ */ jsxRuntime.jsx(w, { summary, appearance: "plain", children: /* @__PURE__ */ jsxRuntime.jsx("div", { style: { display: "flex", flexDirection: "column", gap: 8, padding: "4px 2px", minWidth: 220 }, children: offers.map((o, i) => /* @__PURE__ */ jsxRuntime.jsxs(
      "div",
      {
        style: {
          display: "flex",
          flexDirection: "column",
          gap: 2,
          fontSize: 12,
          paddingTop: i === 0 ? 0 : 8,
          borderTop: i === 0 ? "none" : "1px solid var(--color-beam-border, rgba(127,127,127,0.25))"
        },
        children: [
          offers.length > 1 && /* @__PURE__ */ jsxRuntime.jsxs("span", { style: { color: "var(--color-beam-text-muted)", fontWeight: 600 }, children: [
            "Offer ",
            i + 1
          ] }),
          /* @__PURE__ */ jsxRuntime.jsxs("span", { children: [
            /* @__PURE__ */ jsxRuntime.jsx("strong", { children: "item:" }),
            " ",
            dash(o.itemId)
          ] }),
          /* @__PURE__ */ jsxRuntime.jsxs("span", { children: [
            /* @__PURE__ */ jsxRuntime.jsx("strong", { children: "value:" }),
            " ",
            dash(o.value)
          ] }),
          /* @__PURE__ */ jsxRuntime.jsxs("span", { children: [
            /* @__PURE__ */ jsxRuntime.jsx("strong", { children: "customData:" }),
            " ",
            dash(o.customData)
          ] })
        ]
      },
      i
    )) }) });
  }
  function toOfferView(o) {
    const cd = o?.customData;
    return {
      itemId: o?.itemId != null ? String(o.itemId) : "",
      value: o?.value != null ? String(o.value) : "",
      customData: cd == null ? "" : typeof cd === "string" ? cd : JSON.stringify(cd)
    };
  }
  function parseOffers(r) {
    const raw = r["e.offerData"];
    if (raw) {
      try {
        const parsed = JSON.parse(raw);
        const arr = Array.isArray(parsed) ? parsed : [parsed];
        const views = arr.filter((o) => o != null).map(toOfferView);
        if (views.length) return views;
      } catch {
      }
    }
    const legacy = toOfferView({
      itemId: r["e.offerData.itemId"],
      value: r["e.offerData.value"],
      customData: r["e.offerData.customData"]
    });
    return legacy.itemId || legacy.value || legacy.customData ? [legacy] : [];
  }
  function App({ context }) {
    const beam = ke(context);
    const [tables, setTables] = react.useState([]);
    const [pairs, setPairs] = react.useState([]);
    const [campaign, setCampaign] = react.useState("");
    const [node, setNode] = react.useState("");
    const [optStatus, setOptStatus] = react.useState("loading");
    const [optError, setOptError] = react.useState(null);
    const [dataStatus, setDataStatus] = react.useState("idle");
    const [dataError, setDataError] = react.useState(null);
    const [result, setResult] = react.useState(null);
    const loadData = react.useCallback(
      async (funnelTables, c, n) => {
        if (!beam || !c || !n) return;
        setDataStatus("loading");
        setDataError(null);
        try {
          const where = `WHERE "e.campaignId" = ${sqlStr(c)} AND "e.nodeId" = ${sqlStr(n)}`;
          const perTable = await Promise.all(
            funnelTables.map(
              (t2) => runQuery(beam, `SELECT * FROM ${t2} ${where}`).catch(() => [])
            )
          );
          const rows = perTable.flat();
          const byPlayer = /* @__PURE__ */ new Map();
          const details = [];
          for (const r of rows) {
            const player = r["e.gamerTag"] || r["gamer_tag"] || "(unknown)";
            const ft = r["e.funnelType"] || "";
            if (!byPlayer.has(player)) {
              byPlayer.set(player, { Sent: false, Received: false, Opened: false, Clicked: false, Converted: false });
            }
            if (STAGES.includes(ft)) byPlayer.get(player)[ft] = true;
            details.push({
              funnelType: r["e.funnelType"] || "",
              player,
              deeplink: r["e.deeplink"] || "",
              offers: parseOffers(r),
              campaignData: pickCol(r, "campaignData"),
              cidPid: pickCol(r, "cidPid"),
              time: r["act_time"] || r["act_date"] || ""
            });
          }
          const players = [...byPlayer.entries()].map(([player, reached]) => ({ player, reached })).sort((a, b2) => a.player.localeCompare(b2.player));
          const counts = Object.fromEntries(
            STAGES.map((s) => [s, players.filter((p2) => p2.reached[s]).length])
          );
          details.sort(
            (a, b2) => STAGES.indexOf(a.funnelType) - STAGES.indexOf(b2.funnelType) || a.player.localeCompare(b2.player)
          );
          setResult({ players, details, counts });
          setDataStatus("ready");
        } catch (e2) {
          setDataError(e2 instanceof Error ? e2.message : String(e2));
          setDataStatus("error");
        }
      },
      [beam]
    );
    const loadOptions = react.useCallback(async () => {
      if (!beam) return;
      setOptStatus("loading");
      setOptError(null);
      setResult(null);
      try {
        const funnelTables = await listFunnelTables(beam);
        setTables(funnelTables);
        if (funnelTables.length === 0) {
          setPairs([]);
          setOptStatus("ready");
          return;
        }
        const union = funnelTables.map((t2) => `SELECT DISTINCT "e.campaignId" AS campaignId, "e.nodeId" AS nodeId FROM ${t2}`).join(" UNION ");
        const rows = await runQuery(beam, union);
        const seen = /* @__PURE__ */ new Set();
        const ps = [];
        for (const r of rows) {
          const campaignId = r["campaignId"] || "";
          const nodeId = r["nodeId"] || "";
          if (!campaignId) continue;
          const key = `${campaignId}\0${nodeId}`;
          if (seen.has(key)) continue;
          seen.add(key);
          ps.push({ campaignId, nodeId });
        }
        ps.sort((a, b2) => a.campaignId.localeCompare(b2.campaignId) || a.nodeId.localeCompare(b2.nodeId));
        setPairs(ps);
        setOptStatus("ready");
        if (ps.length > 0) {
          setCampaign(ps[0].campaignId);
          setNode(ps[0].nodeId);
          void loadData(funnelTables, ps[0].campaignId, ps[0].nodeId);
        }
      } catch (e2) {
        setOptError(e2 instanceof Error ? e2.message : String(e2));
        setOptStatus("error");
      }
    }, [beam, loadData]);
    react.useEffect(() => {
      void loadOptions();
    }, [loadOptions]);
    const campaigns = [...new Set(pairs.map((p2) => p2.campaignId))];
    const nodesForCampaign = pairs.filter((p2) => p2.campaignId === campaign).map((p2) => p2.nodeId);
    function onCampaignChange(c) {
      const firstNode = pairs.find((p2) => p2.campaignId === c)?.nodeId ?? "";
      setCampaign(c);
      setNode(firstNode);
      void loadData(tables, c, firstNode);
    }
    function onNodeChange(n) {
      setNode(n);
      void loadData(tables, campaign, n);
    }
    if (!beam) return /* @__PURE__ */ jsxRuntime.jsx(d, {});
    return /* @__PURE__ */ jsxRuntime.jsxs(jsxRuntime.Fragment, { children: [
      /* @__PURE__ */ jsxRuntime.jsx(
        ne,
        {
          label: "Campaign Funnel",
          description: "Per-player push funnel (Sent → Received → Opened → Clicked → Converted), filtered by campaign and node."
        }
      ),
      optStatus === "loading" && /* @__PURE__ */ jsxRuntime.jsx(d, {}),
      optStatus === "error" && /* @__PURE__ */ jsxRuntime.jsxs(y, { variant: "danger", appearance: "filled-outlined", children: [
        "Failed to load campaigns: ",
        optError
      ] }),
      optStatus === "ready" && tables.length === 0 && /* @__PURE__ */ jsxRuntime.jsx(y, { variant: "neutral", appearance: "outlined", children: "No push funnel events ingested yet. Fire a campaign push (or the app’s Track offer buttons); analytics ingestion can lag a few minutes, then Refresh." }),
      optStatus === "ready" && tables.length > 0 && /* @__PURE__ */ jsxRuntime.jsxs("div", { style: { display: "flex", flexDirection: "column", gap: 18 }, children: [
        /* @__PURE__ */ jsxRuntime.jsxs("div", { style: { display: "flex", flexWrap: "wrap", gap: 12, alignItems: "flex-end" }, children: [
          /* @__PURE__ */ jsxRuntime.jsxs("label", { style: { display: "flex", flexDirection: "column", gap: 4, minWidth: 200 }, children: [
            /* @__PURE__ */ jsxRuntime.jsx("span", { style: { fontSize: 12, color: "var(--color-beam-text-muted)" }, children: "Campaign ID" }),
            /* @__PURE__ */ jsxRuntime.jsx(_e, { value: campaign, onValueChange: onCampaignChange, children: campaigns.map((c) => /* @__PURE__ */ jsxRuntime.jsx(I, { value: c, children: c }, c)) })
          ] }),
          /* @__PURE__ */ jsxRuntime.jsxs("label", { style: { display: "flex", flexDirection: "column", gap: 4, minWidth: 200 }, children: [
            /* @__PURE__ */ jsxRuntime.jsx("span", { style: { fontSize: 12, color: "var(--color-beam-text-muted)" }, children: "Node ID" }),
            /* @__PURE__ */ jsxRuntime.jsx(_e, { value: node, onValueChange: onNodeChange, children: nodesForCampaign.map((n) => /* @__PURE__ */ jsxRuntime.jsx(I, { value: n, children: n }, n)) })
          ] }),
          /* @__PURE__ */ jsxRuntime.jsx(p, { variant: "brand", appearance: "filled", loading: dataStatus === "loading", onClick: () => loadOptions(), children: "Refresh" })
        ] }),
        dataStatus === "error" && /* @__PURE__ */ jsxRuntime.jsxs(y, { variant: "danger", appearance: "filled-outlined", children: [
          "Failed to load funnel: ",
          dataError
        ] }),
        dataStatus === "loading" && /* @__PURE__ */ jsxRuntime.jsx(d, {}),
        dataStatus === "ready" && result && /* @__PURE__ */ jsxRuntime.jsxs(jsxRuntime.Fragment, { children: [
          /* @__PURE__ */ jsxRuntime.jsx("div", { style: { display: "flex", flexWrap: "wrap", gap: 10 }, children: STAGES.map((s) => /* @__PURE__ */ jsxRuntime.jsx(b, { appearance: "filled-outlined", style: { flex: "1 1 120px", minWidth: 120 }, children: /* @__PURE__ */ jsxRuntime.jsxs("div", { style: { padding: 14, display: "flex", flexDirection: "column", gap: 2 }, children: [
            /* @__PURE__ */ jsxRuntime.jsx("span", { style: { fontSize: 24, fontWeight: 700, color: "var(--color-beam-text)" }, children: result.counts[s] }),
            /* @__PURE__ */ jsxRuntime.jsx("span", { style: { fontSize: 12, color: "var(--color-beam-text-muted)" }, children: s })
          ] }) }, s)) }),
          /* @__PURE__ */ jsxRuntime.jsxs(
            Te,
            {
              data: result.players,
              tableTitle: `Players in ${campaign} / ${node} — steps reached`,
              card: true,
              rowKey: (r) => r.player,
              emptyMessage: "No players found for this campaign/node.",
              children: [
                /* @__PURE__ */ jsxRuntime.jsx(Se, { field: "player", header: "Player (gamerTag)" }),
                STAGES.map((s) => /* @__PURE__ */ jsxRuntime.jsx(Se, { header: s, align: "center", children: (row) => row.reached[s] ? /* @__PURE__ */ jsxRuntime.jsx(g, { variant: "success", appearance: "filled", children: "✓" }) : /* @__PURE__ */ jsxRuntime.jsx("span", { style: { color: "var(--color-beam-text-muted)" }, children: "✗" }) }, s))
              ]
            }
          ),
          /* @__PURE__ */ jsxRuntime.jsxs(
            Te,
            {
              data: result.details,
              tableTitle: "Event details",
              card: true,
              rowKey: (r, i) => `${r.funnelType}:${r.player}:${i}`,
              emptyMessage: "No events.",
              children: [
                /* @__PURE__ */ jsxRuntime.jsx(Se, { field: "funnelType", header: "Step" }),
                /* @__PURE__ */ jsxRuntime.jsx(Se, { field: "player", header: "Player" }),
                /* @__PURE__ */ jsxRuntime.jsx(Se, { header: "Deeplink", children: (r) => dash(r.deeplink) }),
                /* @__PURE__ */ jsxRuntime.jsx(Se, { header: "Offers", children: (r) => /* @__PURE__ */ jsxRuntime.jsx(OffersCell, { offers: r.offers }) }),
                /* @__PURE__ */ jsxRuntime.jsx(Se, { header: "Campaign data", children: (r) => dash(r.campaignData) }),
                /* @__PURE__ */ jsxRuntime.jsx(Se, { header: "cid.pid", width: "300px", children: (r) => /* @__PURE__ */ jsxRuntime.jsx("span", { style: { whiteSpace: "nowrap" }, children: dash(r.cidPid) }) }),
                /* @__PURE__ */ jsxRuntime.jsx(Se, { header: "Time", children: (r) => dash(r.time) })
              ]
            }
          )
        ] })
      ] })
    ] });
  }
  Ae({ beamId: name, App });
})(window["@beamable/react-19"], window["@beamable/react-dom-client-19"], window["@beamable/react-jsx-runtime-19"]);
