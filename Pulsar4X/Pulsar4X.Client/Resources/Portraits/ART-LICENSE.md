# Portrait art: provenance and licence

Everything under `Portraits/` was made on **20 November 2026** (the Kzari pack on 20 November 2026 as well) by Grok Bot, an AI assistant, **for the Pulsar4X project**. It is original generated artwork, not copied from any existing game, stock set or artist. Style: flat vector illustration, approved by the project owner (Jon Soren). Each pack declares its own author, version and licence in its `species.json`, so third-party packs can be dropped in beside these with their own terms.

Layout: `Portraits/shared/` (species-neutral layers) and `Portraits/species/<id>/` (one folder per pack). See the Phase 2 section of `plans/portraits.md` for the contract.

Raw generator sheets are kept in `sheets/`. The cutting, keying and assembly scripts are in `work/`. Preview images are made with `compose_preview.py`.

Shared method for the art made with the image generator:
1. A built-in AI image generator (Grok Bot's image tool) and some procedural drawing produced a flat teal "mannequin" template bust for each pack: a head, neck and shoulders silhouette on pure magenta.
2. The same generator then drew each part (heads, eyes, mouths and so on) on the template as 3x3 part sheets, using the template and the owner-approved reference sheet as reference images.
3. Python (PIL, NumPy, SciPy, OpenCV) cut the cells and keyed out the magenta and teal to alpha. It removed colour fringes, desaturated and normalised the tintable parts to light grey, and resized to 256x256 RGBA. Every part of a pack is aligned to that pack's template cell.

## Pack: `shared`
- `background/` (3): drawn in code (gradients), neutral colours.
- `insignia/` (3): drawn in code (star, bars, chevrons), light grey, tinted by the faction colour.
- `badge/` (3): drawn in code (ring, shield, hexagon), fixed muted gold.
- Not AI-generated. They are free to use with Pulsar4X.

## Pack: `human` (Human)
- Author: Grok Bot, for Pulsar4X. Version 1.0.0.
- Generated parts: heads 01-06, eyes (split into eye-whites, eyes and brows), nose, mouth, hair, back-hair, facial hair, extras and uniforms.
- The nose is a greyscale shape that takes the skin tint. The mouth is fixed mauve.
- Heads 07 (long, narrow) and 08 (wide, round) are warped from head 01 in code, with head anchor sidecars (`head/NN.json`). The neck and the cheek scar are drawn in code.
- Uniforms have their role colours painted in. They are not tinted, apart from a near-white `uniform` palette.

## Pack: `kzari` (Kzari)
- Author: Grok Bot, for Pulsar4X. Version 1.0.0.
- Concept: an original insectoid species. It has a wedge-shaped carapace head, big faceted compound eyes, mandibles instead of a mouth, no nose, and a crest, frill or antennae instead of hair. It has a thin neck and narrow shoulders, and uniforms cut for that body.
- Generated parts: heads (6), eyes (6), mandibles (6), crest (4), crest-back (frills, 2), extras (7) and uniforms (7). The neck (2) is drawn in code.
- Palettes: `chitin` (body, mandibles and neck), `crest` and `eye`. The uniforms have their role colours painted in.
- It uses the same template-bust method with its own alien mannequin. Tintable parts are greyscale.

## Licence
- The art was generated for Pulsar4X and may be distributed with it under the project's licence. The generator's terms give the user ownership of the output. Jon Soren should confirm that licence choice before release.
- No third-party artwork or fonts were used as input except the owner-approved reference sheet, which was itself generated for this project.
- The generator model version is not exposed by the tool. If parts are regenerated with a different model (for example local Flux or SDXL), record its name and licence here.

## Conventions
- Every PNG is 256x256 RGBA.
- Tintable parts are light grey (about 240-250 with darker shading) so a multiply tint gives the colour.
- Parts with a fixed colour use a layer tint of `none`, or `uniform` with a near-white palette.
