# Diagrams

The images in `../images` are rendered from the Mermaid sources in this directory. Regenerate them after editing a source:

```powershell
npx -y @mermaid-js/mermaid-cli -i dependency-rule.mmd -o ../images/dependency-rule.png -b white -s 2 -c mermaid-config.json
npx -y @mermaid-js/mermaid-cli -i request-journey.mmd -o ../images/request-journey.png -b white -s 2 -c mermaid-config.json
npx -y @mermaid-js/mermaid-cli -i refresh-token-rotation.mmd -o ../images/refresh-token-rotation.png -b white -s 2 -c mermaid-config.json
```

NuGet.org renders Markdown images but not Mermaid code blocks, so the README embeds the rendered PNGs.
