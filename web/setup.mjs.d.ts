export class Setup {
  static getNvmVersion(): string;
  static checkAndUseNvm(version: string): void;
  static getPNPMVersion(): string;
  static run(cmd: string): void;
  static nvmCommandExists(): boolean;
  static compareVersions(v1: string, v2: string): number;
}
