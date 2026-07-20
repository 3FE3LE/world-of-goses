World of Goses style-reference generator v15

Replace prompts.json and generate_lineage_splashes.py in:
  art/world-of-goses-minimax-splash-generator/

Changes:
- all runtime prompts are below the safe 1450-character ceiling
- script validates length before any paid request
- prompt-length errors are not retried with the legacy payload
- age is approximately 20, not a broad range
- stronger skin, eye, hair, hairstyle and tattoo variation
- male uses base reference
- female uses only same-lineage male if present, otherwise base
- medieval/pre-industrial only
- subtle lineage-colored textured background, not white or flat

Validate without spending:
  .\run-fixed.ps1 -DryRun -All

Generate all:
  $env:MINIMAX_API_KEY = "TU_API_KEY"
  .\run-fixed.ps1 -All -Force -Yes
