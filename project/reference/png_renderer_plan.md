# PNG Map Renderer Plan

## Step 1: Water Background + Region Texture Masking (DONE)
Using textures from `assets/map/textures/`, fill the entire background with the water texture. Identify regions, make tiles 128x128. Create a single mask per region and tile the appropriate texture into it — no per-tile seams.

## Step 2: Forest Decals
Ladder on forests using decals from `assets/map/decals/`. Use scatter and clustering math for natural-looking placement.

## Step 3: Settlement Decals
Ladder on settlement decals from `assets/map/decals/poi/settlements/`. Use vignettes around settlement decals so they remain legible even on cluttered backgrounds.

## Step 4: Coastlines
Use cool math stuff to create an interesting, organic coastline between land and water.

## Step 5: Region Background Variation
Use cool math stuff to make the region backgrounds slightly more varied and organic (break up the uniform tiling).

## Step 6: Mountains
Mountains. The final frontier.
