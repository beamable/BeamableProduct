(function(jsxRuntime, react, client, sdk) {
  "use strict";
  function e(e2) {
    window[e2.beamId] = { mount: (t2, n) => e2.onMount(t2, n), unmount: (t2) => e2.onUnmount(t2) };
  }
  const t$1 = { registerExtension: e };
  function t(e2) {
    let t2 = ((typeof globalThis < `u` ? globalThis.__beamPortal?.react : void 0) ?? {})[e2];
    if (!t2) throw Error(`Beam React component "${e2}" is not provided by the host portal. Extensions must run inside the Beamable portal — see https://help.beamable.com/ for the extension setup guide.`);
    return t2;
  }
  function r(n) {
    return react.createElement(t(`BeamSpinner`), n);
  }
  r.displayName = `BeamSpinner`;
  function a(n) {
    return react.createElement(t(`BeamButton`), n);
  }
  a.displayName = `BeamButton`;
  function c(n) {
    return react.createElement(t(`BeamBadge`), n);
  }
  c.displayName = `BeamBadge`;
  function p(n) {
    return react.createElement(t(`BeamCard`), n);
  }
  p.displayName = `BeamCard`;
  function O(n) {
    return react.createElement(t(`BeamTag`), n);
  }
  O.displayName = `BeamTag`;
  function G(n) {
    return react.createElement(t(`BeamPage`), n);
  }
  G.displayName = `BeamPage`;
  function q(n) {
    return react.createElement(t(`BeamPageHeader`), n);
  }
  q.displayName = `BeamPageHeader`;
  function ee(n) {
    return react.createElement(t(`BeamCheckbox`), n);
  }
  ee.displayName = `BeamCheckbox`;
  function te(n) {
    return react.createElement(t(`BeamInput`), n);
  }
  te.displayName = `BeamInput`;
  function le(n) {
    return react.createElement(t(`BeamTextarea`), n);
  }
  le.displayName = `BeamTextarea`;
  function ue(e2) {
    return null;
  }
  ue.displayName = `BeamColumn`;
  function pe(n) {
    return react.createElement(t(`BeamTable`), n);
  }
  pe.displayName = `BeamTable`;
  class PushNotificationServiceClient extends sdk.BeamMicroServiceClient {
    constructor(beam) {
      super(beam);
    }
    get serviceName() {
      return "PushNotificationService";
    }
    async registerDeviceToken(params) {
      return this.request({
        endpoint: "RegisterDeviceToken",
        payload: params,
        withAuth: true
      });
    }
    async unregisterDeviceToken(params) {
      return this.request({
        endpoint: "UnregisterDeviceToken",
        payload: params,
        withAuth: true
      });
    }
    async listMyDevices() {
      return this.request({
        endpoint: "ListMyDevices",
        withAuth: true
      });
    }
    async sendPushToSelf(params) {
      return this.request({
        endpoint: "SendPushToSelf",
        payload: params,
        withAuth: true
      });
    }
    async sendPushToPlayer(params) {
      return this.request({
        endpoint: "SendPushToPlayer",
        payload: params,
        withAuth: true
      });
    }
    async listRegisteredPlayers() {
      return this.request({
        endpoint: "ListRegisteredPlayers",
        withAuth: true
      });
    }
    async checkFcmConfig() {
      return this.request({
        endpoint: "CheckFcmConfig",
        withAuth: true
      });
    }
  }
  function formatUnixSeconds(value) {
    const seconds = Number(value);
    if (!seconds || Number.isNaN(seconds)) return "—";
    return new Date(seconds * 1e3).toLocaleString();
  }
  function App({ context }) {
    const [beam, setBeam] = react.useState(null);
    const [players, setPlayers] = react.useState([]);
    const [rosterLoading, setRosterLoading] = react.useState(false);
    const [rosterError, setRosterError] = react.useState(null);
    const [rosterNote, setRosterNote] = react.useState(null);
    const [selected, setSelected] = react.useState(/* @__PURE__ */ new Set());
    const [playerId, setPlayerId] = react.useState("");
    const [title, setTitle] = react.useState("");
    const [body, setBody] = react.useState("");
    const [deepLink, setDeepLink] = react.useState("");
    const [sending, setSending] = react.useState(false);
    const [sendResult, setSendResult] = react.useState(null);
    const [sendError, setSendError] = react.useState(null);
    react.useEffect(() => {
      let cancelled = false;
      context.beam.then((b) => {
        if (!cancelled) setBeam(b);
      });
      return () => {
        cancelled = true;
      };
    }, [context]);
    const loadRoster = react.useCallback(async () => {
      if (!beam) return;
      setRosterLoading(true);
      setRosterError(null);
      setRosterNote(null);
      try {
        const client2 = new PushNotificationServiceClient(beam);
        const result = await client2.listRegisteredPlayers();
        setPlayers(result.players ?? []);
        setRosterNote(result.message ?? null);
      } catch (err) {
        setRosterError(err instanceof Error ? err.message : String(err));
      } finally {
        setRosterLoading(false);
      }
    }, [beam]);
    react.useEffect(() => {
      if (beam) void loadRoster();
    }, [beam, loadRoster]);
    const toggle = react.useCallback((id) => {
      setSelected((prev) => {
        const next = new Set(prev);
        if (next.has(id)) next.delete(id);
        else next.add(id);
        return next;
      });
    }, []);
    const selectAll = react.useCallback(() => {
      setSelected(new Set(players.map((p2) => String(p2.playerId))));
    }, [players]);
    const clearSelection = react.useCallback(() => setSelected(/* @__PURE__ */ new Set()), []);
    const targets = react.useMemo(() => {
      const t2 = new Set(selected);
      if (playerId.trim()) t2.add(playerId.trim());
      return t2;
    }, [selected, playerId]);
    async function sendPush() {
      if (!beam) return;
      if (targets.size === 0) {
        setSendError("Select at least one player (or type a player ID).");
        return;
      }
      if (!title.trim() && !body.trim()) {
        setSendError("A title or body is required.");
        return;
      }
      setSending(true);
      setSendError(null);
      setSendResult(null);
      const client2 = new PushNotificationServiceClient(beam);
      const payload = { title: title.trim(), body: body.trim(), deepLink: deepLink.trim() };
      const outcomes = await Promise.all(
        [...targets].map(
          (id) => client2.sendPushToPlayer({ playerId: id, ...payload }).then((r2) => ({ id, r: r2 })).catch((e2) => ({ id, err: e2 instanceof Error ? e2.message : String(e2) }))
        )
      );
      const agg = {
        playersAttempted: targets.size,
        playersOk: 0,
        devicesDelivered: 0,
        devicesFailed: 0,
        messages: []
      };
      for (const o of outcomes) {
        if ("err" in o) {
          agg.messages.push(`${o.id}: ${o.err}`);
          continue;
        }
        const r2 = o.r;
        if (r2.success) agg.playersOk++;
        agg.devicesDelivered += r2.succeeded;
        agg.devicesFailed += r2.failed;
        for (const m of r2.messages ?? []) agg.messages.push(`${o.id}: ${m}`);
      }
      setSendResult(agg);
      setSending(false);
      void loadRoster();
    }
    const targetCount = targets.size;
    return /* @__PURE__ */ jsxRuntime.jsxs(G, { children: [
      /* @__PURE__ */ jsxRuntime.jsx(q, { children: "Push Notifications" }),
      /* @__PURE__ */ jsxRuntime.jsxs(p, { style: { marginBottom: 20 }, children: [
        /* @__PURE__ */ jsxRuntime.jsx("h3", { slot: "header", children: "Send a notification" }),
        /* @__PURE__ */ jsxRuntime.jsxs("div", { style: { display: "flex", flexDirection: "column", gap: 12, padding: 18 }, children: [
          /* @__PURE__ */ jsxRuntime.jsx(
            te,
            {
              label: "Player ID (optional)",
              placeholder: "Tick players below, and/or paste a specific ID",
              value: playerId,
              onValueChange: setPlayerId
            }
          ),
          /* @__PURE__ */ jsxRuntime.jsx(te, { label: "Title", placeholder: "Notification title", value: title, onValueChange: setTitle }),
          /* @__PURE__ */ jsxRuntime.jsx(le, { label: "Body", placeholder: "Notification body", rows: 3, value: body, onValueChange: setBody }),
          /* @__PURE__ */ jsxRuntime.jsx(
            te,
            {
              label: "Deep link (optional)",
              placeholder: "e.g. myapp://inbox/42",
              value: deepLink,
              onValueChange: setDeepLink
            }
          ),
          /* @__PURE__ */ jsxRuntime.jsxs("div", { children: [
            /* @__PURE__ */ jsxRuntime.jsx(
              a,
              {
                variant: "brand",
                onClick: sendPush,
                disabled: !beam || sending || targetCount === 0,
                loading: sending,
                children: sending ? "Sending…" : `Send to ${targetCount} player${targetCount === 1 ? "" : "s"}`
              }
            ),
            sendError && /* @__PURE__ */ jsxRuntime.jsx("span", { style: { marginLeft: 12, color: "var(--beam-color-danger-600, #c0392b)" }, children: sendError })
          ] }),
          sendResult && /* @__PURE__ */ jsxRuntime.jsxs("div", { style: { marginTop: 4 }, children: [
            /* @__PURE__ */ jsxRuntime.jsx(c, { variant: sendResult.playersOk === sendResult.playersAttempted ? "success" : "danger", children: sendResult.playersOk === sendResult.playersAttempted ? "Sent" : "Partial" }),
            /* @__PURE__ */ jsxRuntime.jsxs("span", { style: { marginLeft: 10 }, children: [
              "Sent to ",
              sendResult.playersOk,
              "/",
              sendResult.playersAttempted,
              " player(s) —",
              " ",
              sendResult.devicesDelivered,
              " device(s) delivered",
              sendResult.devicesFailed > 0 ? `, ${sendResult.devicesFailed} failed` : ""
            ] }),
            sendResult.messages.length > 0 && /* @__PURE__ */ jsxRuntime.jsx(
              "pre",
              {
                style: {
                  marginTop: 10,
                  padding: 12,
                  background: "var(--beam-color-neutral-100, #f4f4f5)",
                  borderRadius: 4,
                  overflow: "auto",
                  whiteSpace: "pre-wrap"
                },
                children: sendResult.messages.join("\n")
              }
            )
          ] })
        ] })
      ] }),
      /* @__PURE__ */ jsxRuntime.jsxs(p, { children: [
        /* @__PURE__ */ jsxRuntime.jsxs("h3", { slot: "header", children: [
          "Registered players",
          " ",
          rosterLoading && /* @__PURE__ */ jsxRuntime.jsx(r, { style: { marginLeft: 8 } })
        ] }),
        /* @__PURE__ */ jsxRuntime.jsxs("div", { style: { padding: 18 }, children: [
          /* @__PURE__ */ jsxRuntime.jsxs("div", { style: { marginBottom: 12, display: "flex", alignItems: "center", gap: 12, flexWrap: "wrap" }, children: [
            /* @__PURE__ */ jsxRuntime.jsx(a, { onClick: loadRoster, disabled: !beam || rosterLoading, children: "Refresh" }),
            /* @__PURE__ */ jsxRuntime.jsx(a, { appearance: "outlined", onClick: selectAll, disabled: players.length === 0, children: "Select all" }),
            /* @__PURE__ */ jsxRuntime.jsx(a, { appearance: "outlined", onClick: clearSelection, disabled: selected.size === 0, children: "Clear" }),
            /* @__PURE__ */ jsxRuntime.jsxs("span", { style: { fontWeight: 600 }, children: [
              "Selected: ",
              selected.size
            ] }),
            rosterError && /* @__PURE__ */ jsxRuntime.jsx("span", { style: { color: "var(--beam-color-danger-600, #c0392b)" }, children: rosterError }),
            rosterNote && /* @__PURE__ */ jsxRuntime.jsx("span", { style: { fontStyle: "italic" }, children: rosterNote })
          ] }),
          /* @__PURE__ */ jsxRuntime.jsxs(
            pe,
            {
              data: players,
              rowKey: (row) => String(row.playerId),
              emptyMessage: "No players have registered a device yet.",
              loading: rosterLoading,
              loadingMessage: "Loading roster…",
              children: [
                /* @__PURE__ */ jsxRuntime.jsx(
                  ue,
                  {
                    header: "",
                    width: "44px",
                    children: (row) => /* @__PURE__ */ jsxRuntime.jsx(
                      ee,
                      {
                        checked: selected.has(String(row.playerId)),
                        onCheckedChange: () => toggle(String(row.playerId))
                      }
                    )
                  }
                ),
                /* @__PURE__ */ jsxRuntime.jsx(
                  ue,
                  {
                    field: "playerId",
                    header: "Player ID",
                    sortable: true,
                    format: (value) => String(value)
                  }
                ),
                /* @__PURE__ */ jsxRuntime.jsx(ue, { field: "deviceCount", header: "Devices", sortable: true, align: "center" }),
                /* @__PURE__ */ jsxRuntime.jsx(
                  ue,
                  {
                    header: "Push platforms",
                    children: (row) => /* @__PURE__ */ jsxRuntime.jsx("span", { style: { display: "inline-flex", gap: 6 }, children: row.platforms.map((p2) => /* @__PURE__ */ jsxRuntime.jsx(O, { children: p2 }, p2)) })
                  }
                ),
                /* @__PURE__ */ jsxRuntime.jsx(
                  ue,
                  {
                    field: "gamePlatform",
                    header: "Game platform",
                    sortable: true,
                    format: (value) => value ? String(value) : "—"
                  }
                ),
                /* @__PURE__ */ jsxRuntime.jsx(
                  ue,
                  {
                    field: "gameDevice",
                    header: "Device",
                    sortable: true,
                    format: (value) => value ? String(value) : "—"
                  }
                ),
                /* @__PURE__ */ jsxRuntime.jsx(
                  ue,
                  {
                    field: "lastUpdated",
                    header: "Last updated",
                    sortable: true,
                    format: (value) => formatUnixSeconds(value)
                  }
                )
              ]
            }
          )
        ] })
      ] })
    ] });
  }
  t$1.registerExtension({
    beamId: "PushNotifications",
    onMount: (container, context) => {
      const root = client.createRoot(container);
      root.render(
        /* @__PURE__ */ jsxRuntime.jsx(react.StrictMode, { children: /* @__PURE__ */ jsxRuntime.jsx(App, { context }) })
      );
      return root;
    },
    onUnmount: (instance) => {
      instance.unmount();
    }
  });
})(window["@beamable/react-jsx-runtime-19"], window["@beamable/react-19"], window["@beamable/react-dom-client-19"], window["@beamable/sdk-1.1.1"]);
