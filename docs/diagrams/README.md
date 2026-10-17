# Diagrams

The images in `../images` are rendered from the Mermaid sources in this directory. Regenerate them after editing a source:

```powershell
npx -y @mermaid-js/mermaid-cli -i dependency-rule.mmd -o ../images/dependency-rule.png -b white -s 2 -c mermaid-config.json
npx -y @mermaid-js/mermaid-cli -i request-journey.mmd -o ../images/request-journey.png -b white -s 2 -c mermaid-config.json
npx -y @mermaid-js/mermaid-cli -i refresh-token-rotation.mmd -o ../images/refresh-token-rotation.png -b white -s 2 -c mermaid-config.json
```

## README variants

The repository keeps two README files, because NuGet.org renders only Markdown: raw HTML is escaped and Mermaid code blocks are not rendered.

| File | Used by | Header | Diagrams |
| --- | --- | --- | --- |
| `README.md` | GitHub | centered HTML header | native Mermaid code blocks |
| `PACKAGE-README.md` | NuGet.org (`PackageReadmeFile`) | Markdown-only header | the PNGs in `../images` |

Keep the two files in sync: everything below the header is identical except for the three diagram blocks. `docs/**` is excluded from the template package, and the PNGs are referenced through absolute `raw.githubusercontent.com` URLs, which NuGet.org allows.
