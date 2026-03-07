namespace Dreamlands.Rules;

public sealed partial class HaulDef
{
    static IEnumerable<HaulDef> Mountains() => new HaulDef[]
    {
        new()
        {
            Id = "ridge_salt_slab",
            Name = @"Ridge Salt Slab",
            OriginBiome = "mountains",
            DestBiome = "swamp",
            OriginFlavor = @"A ridge family cuts slabs from a private salt vein they're not supposed to have rights to. They sell quiet, through intermediaries, to buyers who don't ask questions.",
            DeliveryFlavor = @"You hand over the slab in the village square and immediately three grandmothers appear to examine it. One scrapes it with her thumbnail. Another licks the edge and nods approvingly. The third announces it's genuine ridge salt, not that coastal trash, and worth the price. The buyer pays you while the grandmothers argue over whether to cure eel or preserve it whole.",
        },
        new()
        {
            Id = "charcoal_bundle",
            Name = @"Charcoal Bundle",
            OriginBiome = "mountains",
            DestBiome = "scrub",
            OriginFlavor = @"The charcoal burner's daughter says her father spent three days on this batch and won't let her sell the rest — claims the mesa folk don't appreciate quality work, but the contract was already signed.",
            DeliveryFlavor = @"The clan metalsmith breaks open the bundle and selects a piece at random. He snaps it cleanly in half, studies the break, then touches his tongue to the fresh surface. He nods once, sets it with the rest, and counts out your payment without comment.",
        },
        new()
        {
            Id = "company_scrip_stack",
            Name = @"Company Scrip Stack",
            OriginBiome = "mountains",
            DestBiome = "mountains",
            OriginFlavor = @"A scholar at the academy is collecting evidence of labor practices in the valley mining companies. He's particularly interested in the denominations and circulation patterns.",
            DeliveryFlavor = @"You hand over the stack and she spreads the scrip across her desk, sorting by issuing company and year. Her fingers move quickly, checking watermarks against lamplight. ""Redemption clauses here, here — both voided within eighteen months."" She makes a notation in her ledger, then counts out your payment in real coin.",
        },
        new()
        {
            Id = "raw_silver_ore_chunk",
            Name = @"Raw Silver Ore Chunk",
            OriginBiome = "mountains",
            DestBiome = "plains",
            OriginFlavor = @"A ridge family needed coin fast after a roof collapse and sold off a specimen their grandfather kept on the mantle. The scholars who usually buy such things weren't interested this time.",
            DeliveryFlavor = @"The assayer breaks off a corner with practiced force, examines the fracture under afternoon light, then scratches it against a test stone. She studies the streak, nods once, and weighs the chunk on her scale. ""Seventy percent, maybe better. Mountain ore always runs clean."" She counts out your payment in silver coins without looking up.",
        },
        new()
        {
            Id = "annotated_legal_codex",
            Name = @"Annotated Legal Codex",
            OriginBiome = "mountains",
            DestBiome = "swamp",
            OriginFlavor = @"The local magistrate died without an heir, and his annotated codex — margin notes from forty years of rulings — is the only copy. A scholar arranged the sale rather than see it pulped.",
            DeliveryFlavor = @"The recipient meets you at the edge of the village, barefoot in the mud, and takes the codex without introduction. She flips through it methodically, pausing at certain pages, her lips moving slightly as though counting something. ""Good. Very good."" She pays you exact coin and walks into the deeper swamp, still reading.",
        },
        new()
        {
            Id = "precision_astrolabe",
            Name = @"Precision Astrolabe",
            OriginBiome = "mountains",
            DestBiome = "scrub",
            OriginFlavor = @"An astronomer at one of the upper academies died without heirs, and her instruments went to the University council. They kept what they needed and sold the rest.",
            DeliveryFlavor = @"The Kesharat surveyor sets the astrolabe on his desk and rotates the rete with one finger, checking each gear's tolerance. He sights through the alidade at a distant peak, makes a small adjustment with a jeweler's tool, then sights again. ""Pre-Lattice Verani work. The graduations are still accurate."" He counts out your payment without looking up from the instrument.",
        },
        new()
        {
            Id = "printed_reform_pamphlets",
            Name = @"Printed Reform Pamphlets",
            OriginBiome = "mountains",
            DestBiome = "plains",
            OriginFlavor = @"A reformist faction among the scholars is distributing their arguments in print—land reform, debt abolition, new voting structures. They're targeting the plains merchants who benefit most from the status quo.",
            DeliveryFlavor = @"You hand the stack to a clerk at the administrative hall. He flips through the first pamphlet, expression carefully neutral, then stamps a receiving form twice and files one copy in a cabinet already thick with similar documents. ""We'll review the content and determine distribution permissions within six weeks."" He slides your payment across the desk without looking up.",
        },
        new()
        {
            Id = "sealed_research_dossier",
            Name = @"Sealed Research Dossier",
            OriginBiome = "mountains",
            DestBiome = "mountains",
            OriginFlavor = @"A scholar's daughter is sending her father's final research to a colleague who helped him years ago. The seal on the dossier is wax, but also a promise.",
            DeliveryFlavor = @"You hand the dossier to a clerk at the observatory, who checks the seal and nods. ""Professor Anselm is in the high country for another month. I'll hold it in his office."" She slides the package onto a shelf already crowded with correspondence and pays you from a lockbox without looking up.",
        },
        new()
        {
            Id = "engineered_retort_glassware",
            Name = @"Engineered Retort Glassware",
            OriginBiome = "mountains",
            DestBiome = "swamp",
            OriginFlavor = @"The alchemist's workshop has been scrambling to produce fever remedies before the warm season arrives. They burned through three sets of glassware already and need replacements within the week.",
            DeliveryFlavor = @"You arrive at a stillhouse where half a dozen Revathi are gathered around a table, arguing in low voices about proportions. The eldest takes the retort from you, holds it to the light, checks the seals. ""The neck is too narrow,"" someone says. ""It'll work,"" another counters. The elder sets it carefully on the table with the other equipment and pays you without looking up from the debate.",
        },
        new()
        {
            Id = "corrected_mine_safety_blueprint",
            Name = @"Corrected Mine Safety Blueprint",
            OriginBiome = "mountains",
            DestBiome = "mountains",
            OriginFlavor = @"A scholar submitted the blueprint as evidence in a grievance case and the magistrate caught errors that would doom any miner who used it. The mining company needs the corrections before their liability deepens.",
            DeliveryFlavor = @"You hand over the corrected blueprint to the foreman, who unrolls it next to the original on his plank table. He runs a finger along the support beam measurements, then stops. ""This says sixteen inches. We already cut to fourteen."" He looks at you like you've brought him a problem instead of a solution. After a long silence, he pays you and rolls both blueprints back up without another word.",
        },
        new()
        {
            Id = "observatory_lens_disc",
            Name = @"Observatory Lens Disc",
            OriginBiome = "mountains",
            DestBiome = "scrub",
            OriginFlavor = @"The observatory director retired after thirty years and left the disc behind, but his successor prefers newer glass and won't mount it. The institution is selling it off rather than let it gather dust.",
            DeliveryFlavor = @"The woman who takes delivery is maybe seventy, her hands shaking slightly as she unwraps the lens. She holds it up to the late afternoon light and her breath catches. ""This is from Kaspar's workshop. I knew him before—"" She stops, wipes the edge with her sleeve, and sets it carefully in a wooden frame already prepared on her table. She counts your payment twice, distracted, her eyes never leaving the glass.",
        },
        new()
        {
            Id = "filed_petition_year_73",
            Name = @"Filed Petition (Year 73)",
            OriginBiome = "mountains",
            DestBiome = "mountains",
            OriginFlavor = @"Someone paid good coin to have a petition filed under a different year's docket. We didn't ask why the date matters.",
            DeliveryFlavor = @"You hand over the packet to a clerk in a back office of the registry. He opens it, glances at the year stamp, and slides it into a drawer already thick with similar documents. His eyes meet yours for a moment before he counts out your payment in silence.",
        },
        new()
        {
            Id = "binding_judgment_writ",
            Name = @"Binding Judgment Writ",
            OriginBiome = "mountains",
            DestBiome = "mountains",
            OriginFlavor = @"The court clerk pushed the finalization through in two days instead of the usual two months — the binding takes effect at month's end and the property changes hands whether the document arrives or not.",
            DeliveryFlavor = @"You hand the writ to a woman standing outside a stone cottage with her daughter beside her. She breaks the seal and reads in silence, her lips moving slightly. When she finishes, she folds it carefully and holds it against her chest. ""It's ours now,"" she says to the girl, who nods once, solemn. She pays you and goes inside without another word.",
        },
        new()
        {
            Id = "wegtafel_fragment",
            Name = @"Wegtafel Fragment",
            OriginBiome = "mountains",
            DestBiome = "plains",
            OriginFlavor = @"A scholar's daughter is sending this piece of an old wayside marker to her father, who retired to the plains last year. He used to study the old road networks.",
            DeliveryFlavor = @"You find him in a small rented room above a grain merchant's office. He takes the fragment and immediately starts brushing at it with a horsehair brush, muttering about erosion patterns. He sets it on his desk next to three similar pieces, comparing the carved lettering. ""Third mile marker from the pass,"" he says, mostly to himself. He pays you without looking up.",
        },
        new()
        {
            Id = "certified_assay_report",
            Name = @"Certified Assay Report",
            OriginBiome = "mountains",
            DestBiome = "plains",
            OriginFlavor = @"The report certifies a claim that turned up richer than expected. The original prospector sold out early, and now someone on the plains wants proof of what they missed.",
            DeliveryFlavor = @"You hand over the report. The merchant unfolds it, scans the mineral content columns, then stops. ""This is last season's report. I paid for the new drift survey."" He taps the date stamp. ""Someone's playing games or you picked up the wrong envelope."" He pays you anyway, mouth tight, already composing a letter.",
        },
        new()
        {
            Id = "stack_of_petition_blanks",
            Name = @"Stack of Petition Blanks",
            OriginBiome = "mountains",
            DestBiome = "plains",
            OriginFlavor = @"A court functionary passed last winter and left behind drawers full of unused blanks. His successor says the peak has too many already, and the plainsmen are always filing something.",
            DeliveryFlavor = @"You hand over the stack to a clerk at the municipal hall. She fans them once, checks the watermark against the light from the window, then runs her finger down the margin to test the ruling. ""Peak standard. We go through a hundred of these a month."" She counts out your payment between approvals of other documents.",
        },
        new()
        {
            Id = "hand_ground_pigment_set",
            Name = @"Hand-Ground Pigment Set",
            OriginBiome = "mountains",
            DestBiome = "plains",
            OriginFlavor = @"A painter died this winter without family to claim his belongings. The landlord bundled what seemed valuable and arranged a sale rather than let it sit.",
            DeliveryFlavor = @"You hand over the wooden case and the merchant opens it on her counter, tilting each vial to catch the light. She wets a fingertip, touches it to the vermilion, and rubs it against her thumb. ""Ground with a granite mortar. See how it doesn't clump?"" She closes the case and counts out your payment without haggling.",
        },
        new()
        {
            Id = "leather_bound_almanac",
            Name = @"Leather-Bound Almanac",
            OriginBiome = "mountains",
            DestBiome = "plains",
            OriginFlavor = @"The printer ordered three hundred almanacs but the binder mis-measured and cut the pages too wide for the covers. Someone down in the plains bought the lot at a discount, but only needed one copy sent ahead.",
            DeliveryFlavor = @"The clerk flips through the almanac, checking the lunar tables and planting calendars. She notices the pages extend slightly past the leather cover and runs her finger along the uneven edge. ""It'll do."" She counts out your payment and shelves it behind her desk.",
        },
        new()
        {
            Id = "roll_of_lead_sheeting",
            Name = @"Roll of Lead Sheeting",
            OriginBiome = "mountains",
            DestBiome = "plains",
            OriginFlavor = @"The sheeting came off a church roof in one of the valley towns — replaced after forty years of weather, but still good enough to sell rather than scrap.",
            DeliveryFlavor = @"The roofer unrolls it across two sawhorses and runs her hand over the surface, checking for soft spots where the metal's gone thin. She finds a crease, flexes it twice, nods. She's already climbing the ladder with it tucked under one arm when her apprentice comes over to pay you.",
        },
        new()
        {
            Id = "tin_of_smelter_s_flux",
            Name = @"Tin of Smelter's Flux",
            OriginBiome = "mountains",
            DestBiome = "scrub",
            OriginFlavor = @"The old smelter kept this tin aside for forty years, always said he'd need it someday. His widow decided someday never came.",
            DeliveryFlavor = @"You arrive at the forge to find three smiths arguing over a cracked crucible, voices rising over the clang of hammering. The youngest takes your tin, pries it open with a blade, and sniffs it. ""This is mountain flux—twice the strength we use."" The eldest leans in, dips a finger in the gray powder, rubs it between his fingers. ""We'll cut it with local ash. It'll work."" He counts out your payment while the argument resumes.",
        },
        new()
        {
            Id = "copper_pipe_sections",
            Name = @"Copper Pipe Sections",
            OriginBiome = "mountains",
            DestBiome = "scrub",
            OriginFlavor = @"The mountain waterworks replaced a section of conduit and sold off the old copper piping for scrap value.",
            DeliveryFlavor = @"You hand over the pipes to a woman sitting cross-legged in the shade of a rail shed. She doesn't weigh them or check the metal. She stacks them carefully beside her, one atop another, then taps each with her knuckle and listens. When she's satisfied, she pays you the exact amount and returns to her listening.",
        },
        new()
        {
            Id = "box_of_iron_nails",
            Name = @"Box of Iron Nails",
            OriginBiome = "mountains",
            DestBiome = "scrub",
            OriginFlavor = @"Someone ordered five boxes of nails but the blacksmith sent five *crates*—enough to roof half the mountain. The buyer paid for what he asked for and left the surplus for resale.",
            DeliveryFlavor = @"You set the box on the carpenter's workbench. She opens it, takes out a single nail, and holds it to the light, checking the taper and the head. She tests the point against her thumbnail, then drops it back in and closes the lid. She counts out your payment in small coins and turns back to her work.",
        },
        new()
        {
            Id = "box_of_slate_shingles",
            Name = @"Box of Slate Shingles",
            OriginBiome = "mountains",
            DestBiome = "scrub",
            OriginFlavor = @"The slates came off an old mine office roof during repairs — worn gray rectangles with chips at the corners and nail holes that tell decades of weather.",
            DeliveryFlavor = @"You arrive at the plateau settlement during a roof repair, and three different relatives immediately cluster around to inspect your delivery. The eldest runs her finger along the worn surface and nods. ""Mountain slate. Better than the clay we've been using."" A younger man holds one up to the light, checking for cracks, while another taps it with his knuckle to test the ring. They argue briefly in their own dialect before handing over your payment.",
        },
        new()
        {
            Id = "cast_bronze_door_pull",
            Name = @"Cast Bronze Door Pull",
            OriginBiome = "mountains",
            DestBiome = "scrub",
            OriginFlavor = @"The old smelter's widow found it half-buried in the yard where her husband's workshop stood before the fire. She won't say why she's selling it, but she wrapped it three times before handing it over.",
            DeliveryFlavor = @"The metalworker sets it on his anvil and taps it with a small hammer, listening to the ring. He turns it over, checking the thickness at the mounting points, then runs his finger along the interior curve where the casting shows its grain. ""Pre-Administration work. Maybe two hundred years old—they poured it hotter than we do now."" He pays you and sets it carefully aside from his other stock.",
        },
        new()
        {
            Id = "pressed_edelweiss_collection",
            Name = @"Pressed Edelweiss Collection",
            OriginBiome = "mountains",
            DestBiome = "swamp",
            OriginFlavor = @"A botanist had pressed samples for a thesis she never finished. The collection's been sitting in a drawer for years, and she'd rather have the space.",
            DeliveryFlavor = @"The buyer unwraps the cloth and examines each flower through a small magnifying lens, checking for rot or discoloration. She nods once, satisfied, and sets them carefully in a wooden case lined with wax paper. She counts out your payment and turns back to her work without another word.",
        },
        new()
        {
            Id = "zinc_casting_mold",
            Name = @"Zinc Casting Mold",
            OriginBiome = "mountains",
            DestBiome = "swamp",
            OriginFlavor = @"The foundry's old mold cracked straight through after years of use. They need a replacement before the next batch of fittings is due.",
            DeliveryFlavor = @"You hand the mold to a young woman at the dock who checks it against a scrap of paper, then wraps it in oilcloth without comment. ""For the metalworker two villages south,"" she says. ""He'll get it when the water's low enough to pass."" She counts out your payment and turns back to sorting crates.",
        },
        new()
        {
            Id = "crucible_set",
            Name = @"Crucible Set",
            OriginBiome = "mountains",
            DestBiome = "swamp",
            OriginFlavor = @"The workshop order came through a middleman who paid double the usual rate and wouldn't say what the crucibles were for. The metalsmith packed them carefully and mentioned he'd prefer not to take another commission from that buyer.",
            DeliveryFlavor = @"The woman who meets you wears Revathi cloth but her hands are stained with chemicals that don't belong in the swamp. She opens the case and taps each crucible with a fingernail, listening to the ring. ""Good thickness,"" she says, then looks at you too long without blinking. She pays exact weight in old imperial coinage that hasn't been minted in twenty years.",
        },
        new()
        {
            Id = "surveyor_s_plumb_weight",
            Name = @"Surveyor's Plumb Weight",
            OriginBiome = "mountains",
            DestBiome = "swamp",
            OriginFlavor = @"A retired surveyor donated his instruments to the university's mathematics collection. The plumb weight didn't match any contemporary standard, and someone thought it might be older Revathi work.",
            DeliveryFlavor = @"The woman who meets you is older, her hands scarred from decades of reed-cutting. She takes the weight and turns it slowly, tracing the incised pattern with one fingertip. ""My grandfather made instruments in the valley cities. I know his mark."" She pays you carefully, counting each coin twice, then wraps the weight in oiled cloth without looking up again.",
        },
        new()
        {
            Id = "bound_court_transcript",
            Name = @"Bound Court Transcript",
            OriginBiome = "mountains",
            DestBiome = "swamp",
            OriginFlavor = @"A mountain court case wrapped up after eight years of deliberation. The losing party requested a certified transcript—not for appeal, but to prove they exhausted every legal avenue before taking the matter elsewhere.",
            DeliveryFlavor = @"The reader sits at a tilework table and cracks the seal without ceremony. She flips through sections, pausing at key passages, her finger tracking margin notes. ""The procedural arguments are competent. The substantive law is garbage."" She closes it and slides your payment across the table. ""Tell me—did they really think this would hold up anywhere that isn't a monastery?""",
        },
        new()
        {
            Id = "iron_brace_joints",
            Name = @"Iron Brace Joints",
            OriginBiome = "mountains",
            DestBiome = "mountains",
            OriginFlavor = @"Someone at the company store let these joints walk out the back door at cost. No paperwork, no questions about what happened to the inventory log.",
            DeliveryFlavor = @"You hand the package to a woman in a mine foreman's coat who opens it in an alley behind the company offices. She counts each piece twice, her lips moving silently, then wraps them back up without testing the fit. She pays you in mixed coin—some scrip, some real money—and walks back inside through the main entrance like she's been there all along.",
        },
        new()
        {
            Id = "mine_cart_axle_pin",
            Name = @"Mine Cart Axle Pin",
            OriginBiome = "mountains",
            DestBiome = "mountains",
            OriginFlavor = @"The old pin sheared clean three weeks ago and the workers have been running loads by hand. The mine cart must be the size of a house.",
            DeliveryFlavor = @"The engineer is waiting for you when you arrive. He takes the pin out of its wood box and stares blankly. ""What am I supposed to do with this? It's four times the size of the fitting."" He insists you take the item back, but a contract is a contract. By the time he's fetched the payment, a crowd of miners has gathered around to offer their opinions on the pin.",
        },
        new()
        {
            Id = "sealed_ore_sample_box",
            Name = @"Sealed Ore Sample Box",
            OriginBiome = "mountains",
            DestBiome = "mountains",
            OriginFlavor = @"A company supervisor sealed samples from a contested vein before the rival outfit could file their claim. Speed matters more than discretion.",
            DeliveryFlavor = @"The clerk at the land office takes the box and checks the seal against a registry. She frowns. ""This was supposed to arrive yesterday. The hearing already happened."" She sets it on a crowded shelf with three other identical boxes, all sealed and logged. ""We'll still need to process it. Standard fee.""",
        },
        new()
        {
            Id = "a_sealed_stift_verdict",
            Name = @"A Sealed Stift Verdict",
            OriginBiome = "mountains",
            DestBiome = "forest",
            OriginFlavor = @"A sealed verdict from the peak courts. Someone paid to have it delivered privately rather than through official channels. The wax is stamped with the sigil of the high bench.",
            DeliveryFlavor = @"You hand the sealed document to a woman standing outside a timber cabin at the forest's edge. She opens the case at once, letting it fall to the ground. Her face goes pale as she reads. She does not meet your gaze. ""Take your coin, outlander,"" she says quietly.",
        },
        new()
        {
            Id = "a_case_for_pessimism",
            Name = @"A Case for Pessimism",
            OriginBiome = "mountains",
            DestBiome = "forest",
            OriginFlavor = @"Banned by the empire. Moral rot or something. Maybe keep it in the bottom of your pack.",
            DeliveryFlavor = @"You hand over the package in a small clearing where three exiles are sharpening tools. One of them breaks the seal and pages through it while the others lean in to read over his shoulder. ""Thought they burned all of these,"" one mutters. The reader closes it carefully, tucks it under his coat, and counts out your payment while the others watch the treeline.",
        },
        new()
        {
            Id = "set_of_steel_traps",
            Name = @"Set of Steel Traps",
            OriginBiome = "mountains",
            DestBiome = "forest",
            OriginFlavor = @"The snows came early this year and a trapper's order sat unfinished through the whole season. Now the thaw's brief and he needs them before the game moves to higher ground.",
            DeliveryFlavor = @"You find the trapper sharpening stakes outside a low cabin. He sets down his work and tests each trap's spring mechanism with his boot, watching the jaw snap shut. He counts out your payment in silence, already turning back to his work before you've pocketed the coins.",
        },
        new()
        {
            Id = "the_illustrated_languilo_volume_2",
            Name = @"The Illustrated Languilo Volume 2",
            OriginBiome = "mountains",
            DestBiome = "forest",
            OriginFlavor = @"This book has been sitting in the storehouse for years, but we've finally found a buyer.",
            DeliveryFlavor = @"A young woman meets you at the forest edge, glancing back toward the settlement. She opens the book to check the illustrations, then rotates the book on its side. ""Larger than I had heard,"" she says pensively. She pays quickly and walks back without another word.",
        },
        new()
        {
            Id = "cast_iron_cookpot",
            Name = @"Cast Iron Cookpot",
            OriginBiome = "mountains",
            DestBiome = "forest",
            OriginFlavor = @"A foundry in the lowlands cast more cookware than the valley towns could buy. A merchant arranged a sale to a settlement that's been relying on clay.",
            DeliveryFlavor = @"The woman takes the pot in both hands, turns it slowly, then sets it on the hearthstone and kneels beside it. She runs her fingers along the rim, presses her palm to the base. ""My mother had one like this,"" she says quietly. She pays you and doesn't look up again.",
        },
        new()
        {
            Id = "woodworking_tools",
            Name = @"Woodworking Tools",
            OriginBiome = "mountains",
            DestBiome = "forest",
            OriginFlavor = @"A carpenter's planer cracked at the tang and the blade chipped beyond sharpening. He's ordered replacements rather than wait for the smith to finish repairs.",
            DeliveryFlavor = @"A boy of maybe twelve meets you at the workshop door. He takes the wrapped bundle and calls back over his shoulder. An older voice from inside tells him to check the chisel edges. He unfolds the cloth, runs his thumbnail carefully across each blade, and nods. He counts out your payment from a pouch at his belt, already walking back inside.",
        },
        new()
        {
            Id = "box_of_glass_jars",
            Name = @"Box of Glass Jars",
            OriginBiome = "mountains",
            DestBiome = "forest",
            OriginFlavor = @"The glassworks produces for the local market. Not sure how we ended up with a case, but we have a buyer.",
            DeliveryFlavor = @"You hand over the box to a woman outside a low timber cabin. She opens it, counts the jars, then holds one up to the light and frowns. ""These are the small ones. I ordered the large."" She argues that the difference matters for preserving whole mushrooms, not sliced. You show her the manifest. She reads it twice, mutters something about the glassworks, and pays you what's owed.",
        },
        new()
        {
            Id = "axehead_mold",
            Name = @"Axehead Mold",
            OriginBiome = "mountains",
            DestBiome = "forest",
            OriginFlavor = @"We do a good trade of axeheads to forest folk, but one of them thinks he can start casting his own.",
            DeliveryFlavor = @"The smith has a stack of bog iron ingots ready to smelt. He inspects the mold with a grin. ""Expensive, but this is the last time those highlanders will rob me."" He pays what's owed.",
        },
    };
}
