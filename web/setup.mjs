import { execSync, spawnSync } from 'node:child_process';
import { readFileSync, existsSync } from 'node:fs';
import process from 'node:process';
import path from 'node:path';

export class Setup {
  static getNvmVersion() {
    const nvmPath = path.resolve('.nvmrc');
    if (existsSync(nvmPath)) {
      return readFileSync(nvmPath, 'utf8').trim().replace(/^v/, '');
    }
    return '22.14.0'; // default fallback version
  }

  static checkAndUseNvm(version) {
    // Get the currently running Node version (remove leading "v")
    const currentNodeVersion = process.version.replace(/^v/, '');
    if (Setup.compareVersions(currentNodeVersion, version) >= 0) {
      console.log(
        `✔ Current Node version (${currentNodeVersion}) satisfies required version (${version}). Skipping nvm install/use.`,
      );
      return;
    } else {
      console.log(
        `❌ Current Node version (${currentNodeVersion}) doesn't meet the required version (${version}). Proceeding to use nvm to install required version.`,
      );
    }

    // Check if nvm is installed on machine
    if (!Setup.nvmCommandExists()) {
      console.error(
        '❌ NVM is not installed. Please install it: https://github.com/nvm-sh/nvm',
      );
      process.exit(1);
    }

    if (process.platform !== 'win32') {
      // Determine NVM_DIR. Fallback to "$HOME/.nvm" if not set.
      const nvmDir =
        process.env.NVM_DIR ||
        path.join(process.env.HOME || process.env.USERPROFILE || '', '.nvm');
      if (!nvmDir || !existsSync(nvmDir)) {
        console.error(
          '❌ NVM directory not found. Please ensure NVM is installed properly.',
        );
        process.exit(1);
      }

      // Launch commands in a login shell so that nvm is loaded.
      Setup.run(`bash --login -c "nvm install ${version}"`);
      Setup.run(`bash --login -c "nvm use ${version}"`);
    } else {
      // For Windows, assume nvm-windows is installed and available in PATH.
      Setup.run(`nvm install ${version}`);
      Setup.run(`nvm use ${version}`);
    }
  }

  static getPNPMVersion() {
    const pkg = JSON.parse(readFileSync('./package.json', 'utf8'));
    const pm = pkg.packageManager;
    if (!pm || !pm.startsWith('pnpm@')) {
      console.error(
        '❌ No valid "packageManager": "pnpm@x.y.z" found in package.json',
      );
      process.exit(1);
    }
    return pm.split('@')[1];
  }

  static run(cmd) {
    console.log(`> ${cmd}`);
    execSync(cmd, { stdio: 'inherit', shell: true });
  }

  static nvmCommandExists() {
    if (process.platform !== 'win32') {
      // On Unix-like systems, NVM is a shell function, so check if NVM_DIR is set.
      return !!process.env.NVM_DIR;
    } else {
      // On Windows, check if nvm is accessible in the PATH.
      const whereResult = spawnSync('where', ['nvm'], { shell: true });
      return whereResult.status === 0;
    }
  }

  // Compares two semantic version strings (e.g. "22.14.0") and returns:
  // 1 if v1 > v2, 0 if equal, -1 if v1 < v2.
  static compareVersions(v1, v2) {
    const parts1 = v1.split('.').map(Number);
    const parts2 = v2.split('.').map(Number);
    for (let i = 0; i < Math.max(parts1.length, parts2.length); i++) {
      const num1 = parts1[i] || 0;
      const num2 = parts2[i] || 0;
      if (num1 > num2) return 1;
      if (num1 < num2) return -1;
    }
    return 0;
  }
}

function main() {
  // 0. Start setup
  console.log('🧭 Starting project setup...');

  // 1. Ensure correct Node.js version via nvm
  console.log('🔧 Using Node version from .nvmrc...');
  const nodeVersion = Setup.getNvmVersion();
  Setup.checkAndUseNvm(nodeVersion);

  // 2. Enable corepack
  const corepackEnableCommand =
    process.platform === 'win32' ? 'corepack enable' : 'sudo corepack enable';
  console.log('🔌 Enabling Corepack...');
  Setup.run(corepackEnableCommand);

  // 3. Prepare specified pnpm version for this project
  const pnpmVersion = Setup.getPNPMVersion();
  console.log(`📦 Using pnpm@${pnpmVersion} for this project...`);
  Setup.run(`corepack prepare pnpm@${pnpmVersion}`);

  // 4. Install dependencies
  console.log('📥 Installing dependencies with pnpm...');
  Setup.run('pnpm install');

  console.log('✅ Setup complete!');
}

if (process.argv[1] === new URL(import.meta.url).pathname) {
  main();
}
