import { readdir, readFile, writeFile } from 'fs/promises';
import path from 'path';

const ASSETS_DIR = './dist/assets';
const DIST_DIR = './dist';

async function inlineAssets() {
  const jsFiles = (await readdir(ASSETS_DIR)).filter(f => f.endsWith('.js'));
  const cssFiles = (await readdir(ASSETS_DIR)).filter(f => f.endsWith('.css'));

  let html = await readFile(path.join(DIST_DIR, 'index.html'), 'utf8');

  // Remove all <script> with src tags and <link rel="stylesheet"> tags
  html = html.replace(/<script type="module" crossorigin src=".*"><\/script>/g, '');
  html = html.replace(/<link rel="stylesheet" crossorigin href=".*">/g, '');

  // Inline CSS
  for (const cssFile of cssFiles) {
    const cssContent = await readFile(path.join(ASSETS_DIR, cssFile), 'utf8');
    html = html.replace('</head>', `<style>${cssContent}</style></head>`);
  }

  // Inline JS
  for (const jsFile of jsFiles) {
    const jsContent = await readFile(path.join(ASSETS_DIR, jsFile), 'utf8');
    html = html.replace('</body>', `<script>${jsContent}</script></body>`);
  }

  // Save combined HTML
  await writeFile(path.join(DIST_DIR, 'combined.html'), html, 'utf8');

  console.log('Assets inlined successfully!');
}

inlineAssets().catch(console.error);
