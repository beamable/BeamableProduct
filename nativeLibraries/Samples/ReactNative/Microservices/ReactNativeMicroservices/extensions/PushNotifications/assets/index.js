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
    async sendCampaignPushToSelf(params) {
      return this.request({
        endpoint: "SendCampaignPushToSelf",
        payload: params,
        withAuth: true
      });
    }
    async sendCampaignPushToPlayer(params) {
      return this.request({
        endpoint: "SendCampaignPushToPlayer",
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
  function kvRowsToJson(rows) {
    const obj = {};
    let any = false;
    for (const r2 of rows) {
      const k = r2.key.trim();
      if (!k) continue;
      obj[k] = r2.value;
      any = true;
    }
    return any ? JSON.stringify(obj) : "";
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
    const [campaignId, setCampaignId] = react.useState("");
    const [nodeId, setNodeId] = react.useState("");
    const [accountId, setAccountId] = react.useState("");
    const [cidPid, setCidPid] = react.useState("");
    const [offers, setOffers] = react.useState([]);
    const [campaignData, setCampaignData] = react.useState([]);
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
    const addOffer = react.useCallback(
      () => setOffers((prev) => [...prev, { itemId: "", value: "", customData: "" }]),
      []
    );
    const removeOffer = react.useCallback(
      (i) => setOffers((prev) => prev.filter((_, idx) => idx !== i)),
      []
    );
    const updateOffer = react.useCallback(
      (i, field, value) => setOffers((prev) => prev.map((o, idx) => idx === i ? { ...o, [field]: value } : o)),
      []
    );
    const addKv = react.useCallback(
      () => setCampaignData((prev) => [...prev, { key: "", value: "" }]),
      []
    );
    const removeKv = react.useCallback(
      (i) => setCampaignData((prev) => prev.filter((_, idx) => idx !== i)),
      []
    );
    const updateKv = react.useCallback(
      (i, field, value) => setCampaignData((prev) => prev.map((r2, idx) => idx === i ? { ...r2, [field]: value } : r2)),
      []
    );
    const targets = react.useMemo(() => {
      const t2 = new Set(selected);
      if (playerId.trim()) t2.add(playerId.trim());
      return t2;
    }, [selected, playerId]);
    const invalidCustomDataIdx = react.useMemo(() => {
      const bad = /* @__PURE__ */ new Set();
      offers.forEach((o, i) => {
        const custom = o.customData.trim();
        if (!custom) return;
        try {
          JSON.parse(custom);
        } catch {
          bad.add(i);
        }
      });
      return bad;
    }, [offers]);
    const hasInvalidCustomData = invalidCustomDataIdx.size > 0;
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
      if (hasInvalidCustomData) {
        setSendError("One or more offers have invalid JSON in Custom data.");
        return;
      }
      setSending(true);
      setSendError(null);
      setSendResult(null);
      const client2 = new PushNotificationServiceClient(beam);
      const builtOffers = offers.filter((o) => o.itemId.trim() || o.value.trim() || o.customData.trim()).map((o) => {
        const offer = { itemId: o.itemId.trim(), value: o.value.trim() };
        const custom = o.customData.trim();
        if (custom) offer.customData = custom;
        return offer;
      });
      const campaignDataJson = kvRowsToJson(campaignData);
      const campaignRequest = {
        title: title.trim(),
        body: body.trim(),
        deepLink: deepLink.trim()
      };
      if (campaignId.trim()) campaignRequest.campaignId = campaignId.trim();
      if (nodeId.trim()) campaignRequest.nodeId = nodeId.trim();
      if (accountId.trim()) campaignRequest.accountId = accountId.trim();
      if (cidPid.trim()) campaignRequest.cidPid = cidPid.trim();
      if (builtOffers.length > 0) campaignRequest.offers = builtOffers;
      if (campaignDataJson) campaignRequest.campaignData = campaignDataJson;
      const outcomes = await Promise.all(
        [...targets].map(
          (id) => client2.sendCampaignPushToPlayer({ playerId: id, ...campaignRequest }).then((r2) => ({ id, r: r2 })).catch((e2) => ({ id, err: e2 instanceof Error ? e2.message : String(e2) }))
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
          /* @__PURE__ */ jsxRuntime.jsxs(
            "div",
            {
              style: {
                borderTop: "1px solid var(--beam-color-neutral-200, #e4e4e7)",
                paddingTop: 12,
                display: "flex",
                flexDirection: "column",
                gap: 12
              },
              children: [
                /* @__PURE__ */ jsxRuntime.jsx("span", { style: { fontWeight: 600 }, children: "Campaign coordinates (optional)" }),
                /* @__PURE__ */ jsxRuntime.jsxs(
                  "div",
                  {
                    style: {
                      display: "grid",
                      gridTemplateColumns: "1fr 1fr",
                      gap: 12
                    },
                    children: [
                      /* @__PURE__ */ jsxRuntime.jsx(te, { label: "Campaign ID", placeholder: "campaignId", value: campaignId, onValueChange: setCampaignId }),
                      /* @__PURE__ */ jsxRuntime.jsx(te, { label: "Node ID", placeholder: "nodeId", value: nodeId, onValueChange: setNodeId }),
                      /* @__PURE__ */ jsxRuntime.jsx(te, { label: "Account ID", placeholder: "accountId", value: accountId, onValueChange: setAccountId }),
                      /* @__PURE__ */ jsxRuntime.jsx(te, { label: "cid.pid", placeholder: "<cid>.<pid> (defaults to MS realm)", value: cidPid, onValueChange: setCidPid })
                    ]
                  }
                ),
                /* @__PURE__ */ jsxRuntime.jsxs("div", { style: { display: "flex", alignItems: "center", gap: 12 }, children: [
                  /* @__PURE__ */ jsxRuntime.jsx("span", { style: { fontWeight: 600 }, children: "Offers" }),
                  /* @__PURE__ */ jsxRuntime.jsx(a, { appearance: "outlined", onClick: addOffer, children: "Add offer" })
                ] }),
                offers.length === 0 ? /* @__PURE__ */ jsxRuntime.jsx("span", { style: { fontStyle: "italic", color: "var(--beam-color-neutral-500, #71717a)" }, children: "No offers." }) : offers.map((o, i) => /* @__PURE__ */ jsxRuntime.jsxs(
                  "div",
                  {
                    style: { display: "grid", gridTemplateColumns: "1fr 1fr 2fr auto", gap: 8, alignItems: "end" },
                    children: [
                      /* @__PURE__ */ jsxRuntime.jsx(
                        te,
                        {
                          label: i === 0 ? "Item ID" : void 0,
                          placeholder: "itemId",
                          value: o.itemId,
                          onValueChange: (v) => updateOffer(i, "itemId", v)
                        }
                      ),
                      /* @__PURE__ */ jsxRuntime.jsx(
                        te,
                        {
                          label: i === 0 ? "Value" : void 0,
                          placeholder: "value",
                          value: o.value,
                          onValueChange: (v) => updateOffer(i, "value", v)
                        }
                      ),
                      /* @__PURE__ */ jsxRuntime.jsxs("div", { style: { display: "flex", flexDirection: "column", gap: 4 }, children: [
                        /* @__PURE__ */ jsxRuntime.jsx(
                          te,
                          {
                            label: i === 0 ? "Custom data (JSON)" : void 0,
                            placeholder: 'e.g. {"tier":"gold"}',
                            value: o.customData,
                            onValueChange: (v) => updateOffer(i, "customData", v),
                            "custom-error": invalidCustomDataIdx.has(i) ? "Invalid JSON" : void 0
                          }
                        ),
                        invalidCustomDataIdx.has(i) && /* @__PURE__ */ jsxRuntime.jsxs("span", { style: { fontSize: 12, color: "var(--beam-color-danger-600, #c0392b)" }, children: [
                          "Custom data must be valid JSON (e.g. ",
                          '{"tier":"gold"}',
                          ")."
                        ] })
                      ] }),
                      /* @__PURE__ */ jsxRuntime.jsx(a, { appearance: "outlined", onClick: () => removeOffer(i), children: "Remove" })
                    ]
                  },
                  i
                )),
                /* @__PURE__ */ jsxRuntime.jsxs("div", { style: { display: "flex", alignItems: "center", gap: 12 }, children: [
                  /* @__PURE__ */ jsxRuntime.jsx("span", { style: { fontWeight: 600 }, children: "Campaign data (key → value)" }),
                  /* @__PURE__ */ jsxRuntime.jsx(a, { appearance: "outlined", onClick: addKv, children: "Add field" })
                ] }),
                campaignData.length === 0 ? /* @__PURE__ */ jsxRuntime.jsx("span", { style: { fontStyle: "italic", color: "var(--beam-color-neutral-500, #71717a)" }, children: "No campaign data fields." }) : campaignData.map((r2, i) => /* @__PURE__ */ jsxRuntime.jsxs("div", { style: { display: "grid", gridTemplateColumns: "1fr 2fr auto", gap: 8, alignItems: "end" }, children: [
                  /* @__PURE__ */ jsxRuntime.jsx(
                    te,
                    {
                      label: i === 0 ? "Key" : void 0,
                      placeholder: "key",
                      value: r2.key,
                      onValueChange: (v) => updateKv(i, "key", v)
                    }
                  ),
                  /* @__PURE__ */ jsxRuntime.jsx(
                    te,
                    {
                      label: i === 0 ? "Value" : void 0,
                      placeholder: "value",
                      value: r2.value,
                      onValueChange: (v) => updateKv(i, "value", v)
                    }
                  ),
                  /* @__PURE__ */ jsxRuntime.jsx(a, { appearance: "outlined", onClick: () => removeKv(i), children: "Remove" })
                ] }, i))
              ]
            }
          ),
          /* @__PURE__ */ jsxRuntime.jsxs("div", { children: [
            /* @__PURE__ */ jsxRuntime.jsx(
              a,
              {
                variant: "brand",
                onClick: sendPush,
                disabled: !beam || sending || targetCount === 0 || hasInvalidCustomData,
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
