function createHashStub(_algorithm: string) {
  return {
    update(_data: string): { digest: (_encoding: string) => string } {
      return this as any;
    },
    digest(_encoding: 'base64'): string {
      return '';
    },
  };
}

// This stub is used to avoid importing the 'crypto' module in environments where it is not available,
// such as in browser environments or when running tests without Node.js.
export const createHash = createHashStub;
