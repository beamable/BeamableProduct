export const objectIdPlaceholder = '{objectId}';
export const GET = 'GET';
export const POST = 'POST';
export const PUT = 'PUT';
export const PATCH = 'PATCH';
export const DELETE = 'DELETE';
export const NodeTokenStorageRequiredMessage = `
Beam SDK initialization failed:
  • In a Node.js environment you **must** supply a token storage implementation.
  • Pass an instance of \`NodeTokenStorage\`, e.g.

      // ESM
      import { NodeTokenStorage } from "@beamable/sdk/platform";
      const beam = new Beam({ tokenStorage: new NodeTokenStorage() });

      // CommonJS
      const { NodeTokenStorage } = require("@beamable/sdk/platform");
      const beam = new Beam({ tokenStorage: new NodeTokenStorage() });

  • Or provide your own class that extends the abstract \`TokenStorage\`.
`.trim();
