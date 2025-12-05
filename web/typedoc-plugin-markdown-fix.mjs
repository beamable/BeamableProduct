import { MarkdownPageEvent } from 'typedoc-plugin-markdown';

export function load(app) {
  app.renderer.on(MarkdownPageEvent.END, (page) => {
    // Remove backslashes before (e.g <T>) from page contents due to mkdocs rendering
    page.contents = page.contents?.replace(/\\</g, '<');
  });
}
