# Portrait layer art: provenance and licence

Applies to everything under `Portraits/` (the layered portrait parts for Pulsar4X) and to the helper scripts in this folder.

## How it was made
- Created on **20 November 2026** by Grok Bot, an AI assistant, **for the Pulsar4X project**. It is original generated artwork, not copied from any existing game, stock set or artist.
- Style: flat vector illustration, approved by the project owner (Jon Soren) from three samples.
- The Portraits/ images came from these steps:
  1. A built-in AI image generator (Grok Bot's image tool) made a bald, front-facing "mannequin" template, a head-and-shoulders bust. That template was redrawn procedurally so that every part uses one geometry.
  2. The same tool then generated part sheets: 3x3 grids with each part drawn on the flat teal template bust over a magenta background. The sheets covered head shapes, eyes with eyebrows, nose and mouth, hairstyles, long hair, uniforms (one per role), facial hair and accessories. Each sheet used the template and the owner-approved reference sheet as reference images.
  3. Python (PIL, NumPy, SciPy, OpenCV; scripts in `work/`) cut the cells, keyed out the magenta and teal to alpha, removed colour fringes, desaturated and normalised the tintable parts to light grey, split the eyes into whites, iris and brows, and resized to 256x256 RGBA. All parts are aligned to the same template cell, so no per-part offsets are needed.
  4. **Drawn in code, not generated:** `background/*` (gradients), `neck/*` (shaped from the template), `insignia/*` (star, bars, chevrons) and `extras/08-scar-cheek.png`.
- Raw generator sheets are kept in `sheets/` for reproducibility.
- Generator model and tools: Grok Bot built-in image generation (the exact underlying model version is not exposed by the tool). The prompts are summarised in step 2.

## Licence
- The art was generated for Pulsar4X and may be distributed with it under the project's licence. The generator's terms give the user ownership of the output. Jon Soren should confirm that licence choice before release.
- No third-party artwork or fonts were used as input except the owner-approved reference sheet, which was itself generated for this project.
- If the layers are regenerated with a different model (for example local Flux or SDXL), record its name and licence here.

## Conventions
- Every PNG is 256x256 RGBA.
- Tintable parts (head, neck, hair, back-hair, facial-hair, brows, eyes) are light grey so a multiply tint gives the colour.
- Parts with fixed colour (uniform, eye-whites, nose-mouth, extras) are not tinted. Insignia are light grey, tinted by the faction colour.
