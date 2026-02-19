# settlements
- name
- region/biome

## map panel
- multiple zoom levels (town/tactical/strategic)
- scale to screen size
- in cli mode, this may just be a list of adjacent tiles (biome and explored status) plus a command to open a full-screen map
- town level should just show usable local buildings. tooltips with function. click to use
- in cli mode, may need an action to switch to town-level "map" (just a list of building and a prompt to pick one or exit the building-picker mode)

## town panel
re-usable panel for town activities. not all settlements will have all buildings.

### default state 
- terse flavor text, CTA to use map 

### temple
- Seek Blessings - minor spirits bump, flavor text. 
- Seek Purification - if you have a qualifying condition. confirm price, remove conditions, flavor text. 
- What do - short diegetic explanation of services provided by the temple

### inn
- Relax - spirits bump, flavor text
- Seek lodging - spirits bump, end-of-day activities (no-biome), skip to morning.
- Buy food - trade screen

### healer
- Seek Mending - confirm price + time, heal to full, clear qualifying conditions, spirits to full, 0-many end-of-day (no-biome)
- Seek Cures - if you have a qualifying condition, confirm price, remove conditions, flavor text. otherwise, flavor text indicating that the healer can only help with diseases and poisoning.
- What do - short diegetic explanation of services provided

### guild outpost
flavor text about the nature, size, and vibe of the guild outpost. introduce the guild factor.
- what do - short introduction to the concept of settlement-local storage, guaranteed safe, can be upgraded. upgrading doesn't make anything larger; you're just paying the factor to reserve more space for your personal crap.
- Store or retrieve items - container interaction
- upgrade - confirm price, add slots, flavor text of factor noting the change in his/her ledger
- gossip - mercantile or negotiation check to get the factor to report what goods are in demand

### market
straight to trade screen



