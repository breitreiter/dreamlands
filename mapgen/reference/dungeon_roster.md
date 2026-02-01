# Dungeon Roster

The canonical source of truth for all dungeons. If it's not in this file, it doesn't exist.

See `dungeon_plan.md` for the overall design philosophy, placement algorithm, and build order.

---

## Biome distribution guide

Target: **20 dungeons** across 5 placeable biomes. Coast and Lake are not navigable terrain — no dungeons there. Each dungeon has one primary biome. Multi-biome compatibility may be added later if placement needs it.

| Biome | Target count | Why |
|-------|-------------|-----|
| Forest | 6 | Most common navigable biome, moderate dead ends. Versatile. |
| Mountains | 5 | Low connectivity = lots of dead ends. Classic dungeon territory. |
| Swamp | 4 | Low connectivity = lots of dead ends. Creepy, hostile, perfect. |
| Hills | 3 | Transition terrain between plains and mountains. Flexible. |
| Plains | 2 | High connectivity, few dead ends. Not zero — barrows and forts exist. |

## Tier distribution

| Tier | Distance | Count | Encounter depth | Design goal |
|------|----------|-------|-----------------|-------------|
| 1 — Safe | 0-15 | 5 | 1-2 encounters | Teach the player what dungeons are. Quick, low stakes. |
| 2 — Frontier | 16-25 | 6 | 2-3 encounters | The bread and butter. Real danger, real rewards. |
| 3 — Wildlands | 26-40 | 6 | 3-4 encounters | Serious expeditions. Ancient, forgotten, dangerous. |
| 4 — Dark Places | 40+ | 3 | 3-5 encounters | Campaign-defining events. Legends. |

---

## The roster

Status key: `[ ]` not started, `[d]` designed (concept firm), `[w]` encounters written, `[a]` art/decal done, `[x]` complete

**Forest**

- **The Librarians' Mound** `[d]`  
  A hill that is not a hill but a buried archive. Roots have cracked the vaults open in places, and the fungus that devours the books has developed a taste for memory.  
  _Folder: `text/encounters/dungeons/librarians_mound/`_  
  _Decal: `assets/map/decals/poi/dungeons/forest/dungeons_pack_aty_normal_31.png`_

- **The Pallid Court** `[d]`  
  A palace of white marble now furred with moss and lichen so thick it breathes. Within, an immortal queen holds revels for courtiers who are merely well-preserved corpses animated by the fungal network threading their bones.  
  _Folder: `text/encounters/dungeons/pallid_court/`_  
  _Decal: `assets/map/decals/poi/dungeons/forest/ruins_megapack_normal_29.png`_

- **The Charnel Garden** `[d]`  
  A sunken amphitheater where an ancient culture displayed their embalmed dead like statuary. The forest has reclaimed most of them, but a few remain upright, and not all of them are entirely dead.  
  _Folder: `text/encounters/dungeons/charnel_garden/`_  
  _Decal: `assets/map/decals/poi/dungeons/forest/ruins_megapack_normal_21.png`_

- **The Fane of Whispering Antlers** `[d]`  
  A temple to a forgotten deer-god, its pillars carved as petrified stags. Pilgrims still come — eyeless things that were once men, drawn by a hymn only they can hear.  
  _Folder: `text/encounters/dungeons/fane_whispering_antlers/`_  
  _Decal: `assets/map/decals/poi/dungeons/forest/dungeons_pack_aty_normal_37.png`_

- **Blackivy Manse** `[d]`  
  A nobleman's estate from a kingdom no one remembers, its walls intact but entirely sheathed in dark ivy. The ivy does not like fire. The interior is pristine, meals laid out on the table, still warm.  
  _Folder: `text/encounters/dungeons/blackivy_manse/`_  
  _Decal: `assets/map/decals/poi/dungeons/forest/dungeons_pack_aty_normal_12.png`_

- **Lammasgate** `[d]`  
  A stone archway standing alone in the forest, covered in effaced carvings. At certain alignments of the stars, it opens onto a marketplace where inhuman merchants sell things that should not be sold. Their currency is years of life.  
  _Folder: `text/encounters/dungeons/lammasgate/`_  
  _Decal: `assets/map/decals/poi/dungeons/forest/ruins_megapack_normal_02.png`_

**Mountain**

- **The Frostwright's Needle** `[d]`  
  A tower of blue-black stone on a peak no path reaches, yet someone clearly built stairs inside it. The top floor contains an orrery that tracks stars no longer in the sky.  
  _Folder: `text/encounters/dungeons/frostwrights_needle/`_  
  _Decal: `assets/map/decals/poi/dungeons/mountains/dungeons_pack_aty_normal_17.png`_

- **Greyspire Hermitage** `[d]`  
  A wizard's tower where the wizard is long dead, but his experiments are not. The tower's rooms rearrange themselves on a schedule only the dead man understood, and something in the basement keeps asking to be fed.  
  _Folder: `text/encounters/dungeons/greyspire_hermitage/`_  
  _Decal: `assets/map/decals/poi/dungeons/mountains/dungeons_pack_aty_normal_09.png`_

- **Marrowpeak Redoubt** `[d]`  
  A fortress built from bones fused with stone. Not human bones — something larger. The garrison vanished but left meticulous journals describing their growing certainty that the fortress was rebuilding itself at night.  
  _Folder: `text/encounters/dungeons/marrowpeak_redoubt/`_  
  _Decal: `assets/map/decals/poi/dungeons/mountains/ruins_megapack_normal_04.png`_

- **Vulture's Pulpit** `[d]`  
  A flat-topped peak where a cult once hurled sacrifices into the clouds. The clouds caught them. On still days, you can hear them up there, alive, calling down for help or company.  
  _Folder: `text/encounters/dungeons/vultures_pulpit/`_  
  _Decal: `assets/map/decals/poi/dungeons/mountains/ruins_megapack_normal_25.png`_

- **The Tabernacle of Honest Men** `[d]`  
  A stone chapel on the mountain run by a small order of monks who have taken a vow of absolute truth. This sounds admirable until you realize their truth includes things mortals shouldn't know, and prolonged exposure to their sermons produces nosebleeds, nightmares, and prophecy.  
  _Folder: `text/encounters/dungeons/tabernacle_honest_men/`_  
  _Decal: `assets/map/decals/poi/dungeons/mountains/ruins_megapack_normal_17.png`_

**Swamp**

- **Submersion of Saint Evarre** `[d]`  
  A chapel to a martyred holy woman, sinking an inch a year for centuries. The nave is waist-deep in black water. Pilgrims say her uncorrupted body still lies behind the altar, but what's down there isn't uncorrupted and it isn't her.  
  _Folder: `text/encounters/dungeons/submersion_saint_evarre/`_  
  _Decal: `assets/map/decals/poi/dungeons/swamp/ruins_megapack_normal_03.png`_

- **Fort Contrition** `[d]`  
  An imperial outpost built to "civilize" the swamp, now half-submerged and listing badly. The garrison's final dispatches describe the walls sweating, the swamp coming up through the floorboards, and the sound of something massive turning over beneath them.  
  _Folder: `text/encounters/dungeons/fort_contrition/`_  
  _Decal: `assets/map/decals/poi/dungeons/swamp/dungeons_pack_aty_normal_01.png`_

- **The Bile Vaults** `[d]`  
  A squat tower with stairs to hidden vaults. The vaults are connected by flooded corridors and contain thousands of sealed ceramic jars, carefully labeled in a dead language, each containing a different preserved organic substance.  
  _Folder: `text/encounters/dungeons/bile_vaults/`_  
  _Decal: `assets/map/decals/poi/dungeons/swamp/dungeons_pack_aty_normal_14.png`_

- **Redoubt of the Mire Baron** `[d]`  
  A robber-lord built his stronghold on what he was told was solid ground. It wasn't. The keep has canted thirty degrees into the bog, creating a disorienting funhouse of tilted corridors and flooded lower chambers. The Baron adapted. His descendants have adapted further.  
  _Folder: `text/encounters/dungeons/redoubt_mire_baron/`_  
  _Decal: `assets/map/decals/poi/dungeons/swamp/dungeons_pack_aty_normal_33.png`_

- **The Fever Palace** `[d]`  
  Once a forest retreat for the nobles of a lost civilization, now calcified with mineral deposits and half-reclaimed by the swamp. Inside, the revelers are still reveling — gaunt, hollow-eyed, dancing to music only they hear, and they'd love for you to join them.  
  _Folder: `text/encounters/dungeons/fever_palace/`_  
  _Decal: `assets/map/decals/poi/dungeons/swamp/ruins_megapack_normal_16.png`_

**Hills**

- **Azharan, the Unruled** `[d]`  
  A city of red sandstone, perfectly preserved, utterly empty. Every building is unlocked. Every table is set for a meal. In the central plaza, a throne sits empty, and anyone who sits in it hears a voice offering them dominion over a kingdom of dust.  
  _Folder: `text/encounters/dungeons/azharan_unruled/`_  
  _Decal: `assets/map/decals/poi/dungeons/hills/ruins_megapack_normal_11.png`_

- **The Caliph's Dreaming** `[d]`  
  A palace that exists only when you fall asleep within a mile of its location. Inside, a mad prince holds court over dreaming travelers, passing increasingly bizarre judgments. Waking up is not always voluntary.  
  _Folder: `text/encounters/dungeons/caliphs_dreaming/`_  
  _Decal: `assets/map/decals/poi/dungeons/hills/ruins_megapack_normal_08.png`_

- **Sepulcher of the Ninety-Nine** `[d]`  
  A step pyramid half-buried in sand, containing the mummified remains of ninety-nine sorcerers entombed together as punishment. The hundredth was the one who sealed them in, and his body is conspicuously absent.  
  _Folder: `text/encounters/dungeons/sepulcher_ninety_nine/`_  
  _Decal: `assets/map/decals/poi/dungeons/hills/dungeons_pack_aty_normal_03.png`_

**Plains**

- **The Bride's Cave** `[d]`  
  A limestone cave system boarded up by the nearest village after a string of disappearances. The locals say a woman in white lures travelers inside. The cave walls are covered in scratched tally marks and love poetry in no known language.  
  _Folder: `text/encounters/dungeons/brides_cave/`_  
  _Decal: `assets/map/decals/poi/dungeons/plains/caveminevintage_normal_17.png`_

- **The Sodality of the Furrow** `[d]`  
  A commune of farmers who worship something beneath their fields. Their crops are impossibly abundant. Their rites involve burying one of their own alive each season, and the buried always return, changed, during the next planting.  
  _Folder: `text/encounters/dungeons/sodality_furrow/`_  
  _Decal: `assets/map/decals/poi/dungeons/plains/ruins_megapack_normal_05.png`_


---

## Biome coverage check

Counting every biome a dungeon can appear in:

| Biome | Dungeons | Count |
|-------|----------|-------|
| Forest | 1, 3, 4, 7, 8, 10, 12, 16, 17, 20 | 10 |
| Hills | 1, 2, 5, 6, 11, 12, 19 | 7 |
| Mountains | 2, 8, 11, 13, 16, 18, 19 | 7 |
| Swamp | 3, 9, 13, 14, 15, 17, 20 | 7 |
| Plains | 5, 6, 7, 10, 14 | 5 |

Forest is highest because it's the most common navigable biome. Plains is lowest because high connectivity means few dead ends. No Coast or Lake — those aren't navigable terrain.

---

## Decisions log

Record design decisions here as they're made, so we don't re-litigate them.

- **2026-02-08**: Roster size set at 20. Each dungeon is unique — one encounter chain, one decal, one placement per map.
- **2026-02-08**: Dungeons are narrative adventures (chained encounters via `[branch]`), not navigable spaces. Scene cut back to world map on completion.
- **2026-02-08**: One and done. Cleared dungeons get a distinct map marker.
- **2026-02-08**: Placement favors dead-end nodes. Graceful skip if no valid placement exists (common on small test maps).
- **2026-02-08**: Each dungeon has a unique map decal (uncleared + cleared variant).
- **2026-02-08**: No coastal or lake dungeons. Coast and Lake are rendering biomes, not navigable terrain.
