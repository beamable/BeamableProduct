import { exec } from 'child_process';
import { promises as fs } from 'fs';
import path from 'path';

// Configuration: Define the main entry point and output settings
const entryFile = 'src/index.ts';
const tsconfig = 'tsconfig.json';
const outDir = 'docs';
const alias = { '@': './src' }; // Maps alias to project path
const excludePatterns = ['**/utils/**'].join(', ');

async function generateDocs() {
  console.log('Reading public API from:', entryFile);

  // Read the main entry file to find all exported modules
  const fileContent = await fs.readFile(entryFile, 'utf-8');

  // Regular expression to match export statements like "export * from './module'"
  // This captures the module path inside quotes after "from"
  const exportRegex = /export .* from ['"](.*?)['"]/g;
  let match;

  // Start with predefined entry points that should always be documented
  const entryPoints = new Set(['src/schema.ts', 'src/api.ts']);

  // Loop through all export statements found in the entry file
  while ((match = exportRegex.exec(fileContent)) !== null) {
    let modulePath = match[1]; // Extract the module path from the regex match

    // Handle alias resolution: convert '@/' to actual './src/' path
    if (modulePath.startsWith('@/')) {
      modulePath = modulePath.replace('@/', alias['@'] + '/');
    } else {
      // Resolve relative paths to absolute paths based on entry file location
      modulePath = path.resolve(path.dirname(entryFile), modulePath);
    }

    // Check if the resolved path exists and determine if it's a directory or file
    try {
      const stat = await fs.stat(modulePath);
      if (stat.isDirectory()) {
        // If it's a directory, add it directly
        entryPoints.add(modulePath);
      }
    } catch (e) {
      // If path doesn't exist, assume it's a TypeScript file without extension
      entryPoints.add(`${modulePath}.ts`);
    }
  }

  // Convert the Set of entry points to a space-separated string for the command
  const entryPointsString = [...entryPoints].join(' ');
  console.log(`Found ${entryPoints.size} total entry points to document.`);

  // Safety check: ensure we have entry points before proceeding
  if (entryPoints.size === 0) {
    console.error('No entry points found. Check your index.ts file.');
    return;
  }

  // Build the TypeDoc command with all necessary flags
  const command = `
    typedoc ${entryPointsString} \
      --out ${outDir} \
      --tsconfig ${tsconfig} \
      --plugin typedoc-plugin-markdown \
      --plugin ./typedoc-plugin-markdown-fix.mjs \
      --intentionallyNotExported ExtractPrefixes \
      --exclude [${excludePatterns}] \
      --excludePrivate \
      --excludeProtected \
      --excludeInternal \
      --excludeReferences \
      --hideGenerator \
      --hidePageHeader
  `;

  console.log('Executing TypeDoc...');

  // Execute the TypeDoc command as a child process
  // The replace() call normalizes whitespace in the command string
  const child = exec(command.replace(/\s+/g, ' ').trim());

  // Pipe the child process output to the main process so we can see progress
  child.stdout.pipe(process.stdout);
  child.stderr.pipe(process.stderr);

  // Handle the completion of the TypeDoc process
  child.on('exit', (code) => {
    console.log(`TypeDoc process exited with code ${code}`);
  });
}

// Run the documentation generation and handle any errors
generateDocs().catch(console.error);
