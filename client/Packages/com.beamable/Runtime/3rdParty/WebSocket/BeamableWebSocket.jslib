
var BeamableLibraryWebsocket = {
	$beamWebSocketState: {
		/*
		 * Map of instances
		 *
		 * Instance structure:
		 * {
		 * 	url: string,
		 * 	ws: WebSocket
		 * }
		 */
		instances: {},

		/* Last instance ID */
		lastId: 0,

		/* Event listeners */
		onOpen: null,
		onMesssage: null,
		onError: null,
		onClose: null,

		/* Debug mode */
		debug: false
	},

	/**
	 * Set onOpen callback
	 *
	 * @param callback Reference to C# static function
	 */
	BeamableWebSocketSetOnOpen: function(callback) {

		beamWebSocketState.onOpen = callback;

	},

	/**
	 * Set onMessage callback
	 *
	 * @param callback Reference to C# static function
	 */
	BeamableWebSocketSetOnMessage: function(callback) {

		beamWebSocketState.onMessage = callback;

	},

	/**
	 * Set onError callback
	 *
	 * @param callback Reference to C# static function
	 */
	BeamableWebSocketSetOnError: function(callback) {

		beamWebSocketState.onError = callback;

	},

	/**
	 * Set onClose callback
	 *
	 * @param callback Reference to C# static function
	 */
	BeamableWebSocketSetOnClose: function(callback) {

		beamWebSocketState.onClose = callback;

	},

	/**
	 * Allocate new WebSocket instance struct
	 *
	 * @param url Server URL
	 */
	BeamableWebSocketAllocate: function(url) {

		var urlStr = UTF8ToString(url);
		var id = beamWebSocketState.lastId++;

		beamWebSocketState.instances[id] = {
		  subprotocols: [],
			url: urlStr,
			ws: null
		};

		return id;

	},

  /**
   * Add subprotocol to instance
   *
   * @param instanceId Instance ID
   * @param subprotocol Subprotocol name to add to instance
   */
  BeamableWebSocketAddSubProtocol: function(instanceId, subprotocol) {

    var subprotocolStr = UTF8ToString(subprotocol);
    beamWebSocketState.instances[instanceId].subprotocols.push(subprotocolStr);

  },

	/**
	 * Remove reference to WebSocket instance
	 *
	 * If socket is not closed function will close it but onClose event will not be emitted because
	 * this function should be invoked by C# WebSocket destructor.
	 *
	 * @param instanceId Instance ID
	 */
	BeamableWebSocketFree: function(instanceId) {

		var instance = beamWebSocketState.instances[instanceId];

		if (!instance) return 0;

		// Close if not closed
		if (instance.ws && instance.ws.readyState < 2)
			instance.ws.close();

		// Remove reference
		delete beamWebSocketState.instances[instanceId];

		return 0;

	},

	/**
	 * Connect WebSocket to the server
	 *
	 * @param instanceId Instance ID
	 */
	BeamableWebSocketConnect: function(instanceId) {

		var instance = beamWebSocketState.instances[instanceId];
		if (!instance) return -1;

		if (instance.ws !== null)
			return -2;

		instance.ws = new WebSocket(instance.url, instance.subprotocols);

		instance.ws.binaryType = 'arraybuffer';

		instance.ws.onopen = function() {

			if (beamWebSocketState.debug)
				console.log("[JSLIB WebSocket] Connected.");

			if (beamWebSocketState.onOpen)
				Module.dynCall_vi(beamWebSocketState.onOpen, instanceId);

		};

		instance.ws.onmessage = function(ev) {

			if (beamWebSocketState.debug)
				console.log("[JSLIB WebSocket] Received message:", ev.data);

			if (beamWebSocketState.onMessage === null)
				return;

			if (ev.data instanceof ArrayBuffer) {

				var dataBuffer = new Uint8Array(ev.data);

				var buffer = _malloc(dataBuffer.length);
				HEAPU8.set(dataBuffer, buffer);

				try {
					Module.dynCall_viii(beamWebSocketState.onMessage, instanceId, buffer, dataBuffer.length);
				} finally {
					_free(buffer);
				}

      } else {
				var dataBuffer = (new TextEncoder()).encode(ev.data);

				var buffer = _malloc(dataBuffer.length);
				HEAPU8.set(dataBuffer, buffer);

				try {
					Module.dynCall_viii(beamWebSocketState.onMessage, instanceId, buffer, dataBuffer.length);
				} finally {
					_free(buffer);
				}

      }

		};

		instance.ws.onerror = function(ev) {

			if (beamWebSocketState.debug)
				console.log("[JSLIB WebSocket] Error occured.");

			if (beamWebSocketState.onError) {

				var msg = "WebSocket error.";
				var length = lengthBytesUTF8(msg) + 1;
				var buffer = _malloc(length);
				stringToUTF8(msg, buffer, length);

				try {
					Module.dynCall_vii(beamWebSocketState.onError, instanceId, buffer);
				} finally {
					_free(buffer);
				}

			}

		};

		instance.ws.onclose = function(ev) {

			if (beamWebSocketState.debug)
				console.log("[JSLIB WebSocket] Closed.");

			if (beamWebSocketState.onClose)
				Module.dynCall_vii(beamWebSocketState.onClose, instanceId, ev.code);

			delete instance.ws;

		};

		return 0;

	},

	/**
	 * Close WebSocket connection
	 *
	 * @param instanceId Instance ID
	 * @param code Close status code
	 * @param reasonPtr Pointer to reason string
	 */
	BeamableWebSocketClose: function(instanceId, code, reasonPtr) {

		var instance = beamWebSocketState.instances[instanceId];
		if (!instance) return -1;

		if (!instance.ws)
			return -3;

		if (instance.ws.readyState === 2)
			return -4;

		if (instance.ws.readyState === 3)
			return -5;

		var reason = ( reasonPtr ? UTF8ToString(reasonPtr) : undefined );

		try {
			instance.ws.close(code, reason);
		} catch(err) {
			return -7;
		}

		return 0;

	},

	/**
	 * Send message over WebSocket
	 *
	 * @param instanceId Instance ID
	 * @param bufferPtr Pointer to the message buffer
	 * @param length Length of the message in the buffer
	 */
	BeamableWebSocketSend: function(instanceId, bufferPtr, length) {

		var instance = beamWebSocketState.instances[instanceId];
		if (!instance) return -1;

		if (!instance.ws)
			return -3;

		if (instance.ws.readyState !== 1)
			return -6;

		instance.ws.send(HEAPU8.buffer.slice(bufferPtr, bufferPtr + length));

		return 0;

	},

	/**
	 * Send text message over WebSocket
	 *
	 * @param instanceId Instance ID
	 * @param bufferPtr Pointer to the message buffer
	 * @param length Length of the message in the buffer
	 */
	BeamableWebSocketSendText: function(instanceId, message) {

		var instance = beamWebSocketState.instances[instanceId];
		if (!instance) return -1;

		if (!instance.ws)
			return -3;

		if (instance.ws.readyState !== 1)
			return -6;

		instance.ws.send(UTF8ToString(message));

		return 0;

	},

	/**
	 * Return WebSocket readyState
	 *
	 * @param instanceId Instance ID
	 */
	BeamableWebSocketGetState: function(instanceId) {

		var instance = beamWebSocketState.instances[instanceId];
		if (!instance) return -1;

		if (instance.ws)
			return instance.ws.readyState;
		else
			return 3;

	}

};

autoAddDeps(BeamableLibraryWebsocket, '$beamWebSocketState');
mergeInto(LibraryManager.library, BeamableLibraryWebsocket);
