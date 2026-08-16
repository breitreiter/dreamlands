import { useState } from "react";

const sections = [
  { id: "character", label: "Your Character" },
  { id: "advancement", label: "Advancement" },
  { id: "skills", label: "Skills" },
  { id: "checks", label: "Skill Checks" },
  { id: "combat", label: "Combat" },
  { id: "inventory", label: "Equipment & Inventory" },
  { id: "conditions", label: "Conditions" },
  { id: "road", label: "The Road" },
  { id: "endofday", label: "End of Day" },
  { id: "settlements", label: "Settlements" },
  { id: "contracts", label: "Contracts" },
];

function Section({ id, title, children }: { id: string; title: string; children: React.ReactNode }) {
  return (
    <section id={id} className="scroll-mt-8">
      <h2 className="font-header text-[32px] text-accent mb-4 border-b border-edge pb-2">{title}</h2>
      <div className="space-y-4">{children}</div>
    </section>
  );
}

function Table({ headers, rows }: { headers: string[]; rows: (string | React.ReactNode)[][] }) {
  return (
    <table className="w-full border-collapse">
      <thead>
        <tr>
          {headers.map((h) => (
            <th key={h} className="text-left px-3 py-2 border-b border-edge text-dim font-bold">{h}</th>
          ))}
        </tr>
      </thead>
      <tbody>
        {rows.map((row, i) => (
          <tr key={i} className={i % 2 === 0 ? "bg-panel/30" : ""}>
            {row.map((cell, j) => (
              <td key={j} className="px-3 py-2 border-b border-edge/50">{cell}</td>
            ))}
          </tr>
        ))}
      </tbody>
    </table>
  );
}

function Stat({ label, value, note }: { label: string; value: string; note: string }) {
  return (
    <div className="bg-panel/50 rounded-lg p-4 border border-edge/50">
      <div className="text-dim font-bold mb-1">{label}</div>
      <div className="text-accent font-bold">{value}</div>
      <div className="text-muted mt-1">{note}</div>
    </div>
  );
}

function Sub({ children }: { children: React.ReactNode }) {
  return <h3 className="text-dim font-bold mt-6 mb-2">{children}</h3>;
}

export default function Reference() {
  const [tocOpen, setTocOpen] = useState(false);

  return (
    <div className="min-h-screen bg-page text-primary">
      {/* Header */}
      <header className="border-b border-edge px-6 py-6 text-center">
        <h1 className="font-header text-[32px] text-accent tracking-wider">Player Reference</h1>
        <p className="text-dim mt-2">A guide to the mechanics and systems of The Merchant</p>
      </header>

      <div className="max-w-5xl mx-auto flex">
        {/* TOC sidebar (desktop) */}
        <nav className="hidden lg:block w-56 shrink-0 sticky top-0 h-screen overflow-y-auto py-8 pr-6">
          <ul className="space-y-2">
            {sections.map((s) => (
              <li key={s.id}>
                <a href={`#${s.id}`} className="text-action hover:text-action-hover transition-colors">
                  {s.label}
                </a>
              </li>
            ))}
          </ul>
        </nav>

        {/* TOC mobile toggle */}
        <div className="lg:hidden fixed top-4 right-4 z-50">
          <button
            onClick={() => setTocOpen(!tocOpen)}
            className="bg-panel border border-edge rounded-lg px-3 py-2 text-action"
          >
            {tocOpen ? "Close" : "Contents"}
          </button>
          {tocOpen && (
            <nav className="absolute right-0 mt-2 bg-panel border border-edge rounded-lg p-4 shadow-lg">
              <ul className="space-y-2">
                {sections.map((s) => (
                  <li key={s.id}>
                    <a
                      href={`#${s.id}`}
                      className="text-action hover:text-action-hover"
                      onClick={() => setTocOpen(false)}
                    >
                      {s.label}
                    </a>
                  </li>
                ))}
              </ul>
            </nav>
          )}
        </div>

        {/* Content */}
        <main className="flex-1 py-8 px-6 space-y-12">

          {/* YOUR CHARACTER */}
          <Section id="character" title="Your Character">
            <p>
              You are a travelling merchant of the Traders Guild, working the imperial borderlands.
              Four numbers describe you: what you can take, what you can bear, what you can spend,
              and what you can carry.
            </p>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Stat label="Health" value="4" note="The last resort. Untreated conditions take it a point per night, and combat only reaches it once your spirits are spent. Reach 0 and you are rescued." />
              <Stat label="Spirits" value="20" note="Morale and stamina in one pool. The road drains it, blows land on it first, and only an inn or a Recover refills it." />
              <Stat label="Gold" value="50" note="Spent on gear, medicine, food, and beds. Earned from contracts, encounters, and selling what you find." />
              <Stat label="Pack slots" value="8" note="Everything you own occupies one slot: weapon, armor, tools, medicine, rations, and each undelivered contract." />
            </div>
            <p className="text-muted">
              Damage in a fight is taken out of spirits first and only bites into health when spirits
              run out. Spirits are the buffer you spend; health is the one you protect.
            </p>
          </Section>

          {/* ADVANCEMENT */}
          <Section id="advancement" title="Advancement">
            <p>
              You do not level up by fighting, trading, or walking. You level up by{" "}
              <strong className="text-dim">going somewhere</strong>. Scattered across the map are
              sites that are not settlements — ruins, camps, works, holdings. Each holds a
              questline. Travel there, enter it, and see it through to the end, and you gain a level.
            </p>
            <p>
              Every level opens the <strong className="text-dim">tableau</strong>: pick one upgrade.
              Each track can be taken twice and no further, so nothing stacks past a second step and
              you cannot max everything in one run.
            </p>
            <Table
              headers={["Track", "First pick", "Second pick"]}
              rows={[
                ["Combat", "Axes and medium armor", "Swords and heavy armor"],
                ["Negotiation", "Contracts pay 20% more", "Contracts pay 40% more"],
                ["Bushcraft", "Halves travel costs; eat every other night", "Quarters travel costs"],
                ["Cunning", "40% to resist serious conditions", "80% to resist serious conditions"],
                ["Constitution", "+1 max health", "+1 max health"],
                ["Packing", "+1 pack slot", "+1 pack slot"],
              ]}
            />
            <p className="text-muted">
              A few questlines hand you a legendary weapon, armor, or tool instead of a tableau pick.
              Those items are yours permanently and are never lost to a rescue.
            </p>
          </Section>

          {/* SKILLS */}
          <Section id="skills" title="Skills">
            <p>
              Four skills, three tiers each: <span className="text-accent">Untrained</span>,{" "}
              <span className="text-accent">Trained</span>, <span className="text-accent">Expert</span>.
              There are no numbers and no experience bar. A skill moves only when you spend a
              tableau pick on it.
            </p>
            <Table
              headers={["Skill", "What it governs", "Trained", "Expert"]}
              rows={[
                ["Combat", "Violence, and the gear you are fit to use", "Axes, medium armor", "Swords, heavy armor"],
                ["Negotiation", "Persuasion, bartering, social footwork", "+20% contract payout", "+40% contract payout"],
                ["Bushcraft", "Wilderness travel, weather, rations", "Half travel costs, eat every other night", "Quarter travel costs"],
                ["Cunning", "Awareness, trickery, staying whole", "40% resist serious conditions", "80% resist serious conditions"],
              ]}
            />
            <p className="text-muted">
              Every tier also improves how skill checks resolve, in every skill. See below.
            </p>
          </Section>

          {/* SKILL CHECKS */}
          <Section id="checks" title="Skill Checks">
            <p>
              There are no dice rolls against a difficulty number. When a check comes up you are
              offered <strong className="text-dim">three approaches</strong>, and you choose one.
              One of the three is the right read on the situation, one is a mistake, and one is
              somewhere in between.
            </p>
            <Table
              headers={["Skill", "Approaches"]}
              rows={[
                ["Combat", "Rush · Strategize · Outlast"],
                ["Negotiation", "Charm · Reason · Threaten"],
                ["Bushcraft", "Push · Plan · Reroute"],
                ["Cunning", "Hide · Bluff · Scheme"],
              ]}
            />
            <p>
              Which approach is correct depends entirely on the scene, not on the skill. Your tier
              decides how much slack you get for reading it wrong:
            </p>
            <Table
              headers={["", "Best approach", "Middle approach", "Worst approach"]}
              rows={[
                [<strong key="u">Untrained</strong>, "50/50 — you may still flub it", "Fail", "Fail"],
                [<strong key="t">Trained</strong>, "Succeed", "Fail", "Fail"],
                [<strong key="e">Expert</strong>, "Succeed", "Succeed", "Fail"],
              ]}
            />
            <p className="text-muted">
              So training does not make you luckier — it makes your judgement count. Untrained, even
              a correct read fails half the time; Expert, craft carries you through a merely decent
              read. Nobody survives the wrong approach.
            </p>
          </Section>

          {/* COMBAT */}
          <Section id="combat" title="Combat">
            <p>
              Fights are a prediction game. Each turn you and your enemy secretly commit{" "}
              <strong className="text-dim">three actions</strong> to three slots. The slots then
              resolve in order, one against one, and neither side can change their mind partway
              through.
            </p>

            <Sub>The triangle</Sub>
            <p>Every base action beats one other and loses to a third.</p>
            <Table
              headers={["Action", "What it does", "Beaten by"]}
              rows={[
                ["Attack", "4 damage. Against a Recover it cancels the heal outright and stuns them.", "Defend"],
                ["Defend", "Caps whatever lands on you at 2, and strikes an attacker back for 4.", "Recover"],
                ["Recover", "Heals 4 spirits — but only if nobody is swinging at you.", "Attack"],
                ["Read Intent", "Reveals the enemy's full plan for next turn. Costs you the slot.", "Anything aggressive"],
              ]}
            />
            <p className="text-muted">
              A guard is not an off-switch — a trickle still gets through — but it does hit back,
              which is what makes leading with Attack every turn a losing habit.
            </p>

            <Sub>Where your moves come from</Sub>
            <p>
              Your move list is your gear. Recover and Read Intent are always yours; everything else
              is granted by what you have equipped.
            </p>
            <ul className="list-disc list-inside space-y-2 ml-2">
              <li>
                <strong className="text-dim">Your weapon supplies every Attack you have.</strong>{" "}
                With no weapon equipped you cannot attack at all. Some fine weapons drop the plain
                swing entirely in favour of specialist strikes.
              </li>
              <li>
                <strong className="text-dim">Your armor supplies every Defend you have.</strong>{" "}
                Unarmored you fall back on a plain Defend, which is better than wearing armor that
                grants none.
              </li>
              <li>
                Better gear does not deal more damage in some invisible way — it hands you{" "}
                <strong className="text-dim">named special moves</strong>, and those are the whole
                advantage. A Pommel Stun, a Perfect Block, a Riposte.
              </li>
            </ul>

            <Sub>What the special moves do</Sub>
            <Table
              headers={["Rider", "Effect"]}
              rows={[
                ["Heavy", "Attack: +4 damage. Defend: caps damage at 1. Recover: +2 healing."],
                ["Perfect", "Defend takes no damage at all."],
                ["Shielding", "Defend nullifies stuns and conditions aimed at you."],
                ["Riposte", "Counters an incoming attack: prevents 2 and adds 2."],
                ["Stunning", "Chance to skip the target's next slot."],
                ["Brutal / Venomous / Glowing / Tainted", "Chance to inflict Injured, Poisoned, Irradiated, or Lattice Sickness."],
                ["Provoking / Terrifying", "Berzerk or Fear: narrows the target's move pool next turn."],
                ["Exhausting", "Hits hard, then stuns you."],
                ["Power / Slow", "Once per turn / once every other turn."],
              ]}
            />

            <Sub>Reading the enemy</Sub>
            <p>
              Every turn opens with a <strong className="text-dim">tell</strong> — one line hinting
              at the shape of the enemy's commitment. Pressing the attack, on their back foot,
              winded, or winding up something heavy. Play Read Intent and you see their exact three
              moves before you commit next turn.
            </p>

            <Sub>Stuns and flight</Sub>
            <p>
              A stun turns the victim's next slot into nothing. If it lands on the third slot it
              carries into the first slot of the following turn. Fleeing ends the fight, but it
              spends the turn: the enemy's three moves still resolve against you while you go.
            </p>
          </Section>

          {/* EQUIPMENT & INVENTORY */}
          <Section id="inventory" title="Equipment & Inventory">
            <p>
              There is one container: your pack, <span className="text-accent">8 slots</span> to
              start and at most 10. There is no separate food bag and no free storage on your
              person. <strong className="text-dim">Almost everything takes a slot</strong> — your
              equipped weapon and armor, every tool and medicine kit, every day's rations, and every
              contract you are carrying. Buy carelessly at market and you will find yourself with no
              room for food.
            </p>

            <Sub>What lives in the pack</Sub>
            <Table
              headers={["Type", "Notes"]}
              rows={[
                ["Weapon", "One equipped at a time. Grants your Attack moves."],
                ["Armor", "One equipped at a time. Grants your Defend moves."],
                ["Tools", "Work passively while carried — waterskin, bedroll, cartographer's kit, lantern."],
                ["Medicine kits", "Treat one condition each, reusable forever, but each occupies a slot for the whole journey."],
                ["Rations", "One slot each, one eaten per night. 3 gold apiece."],
                ["Contracts", "One slot each until delivered."],
              ]}
            />

            <Sub>Combat requirements</Sub>
            <p>
              The good gear expects a fighter. Daggers and light armor ask nothing of you; axes and
              medium armor require <span className="text-accent">Trained</span> Combat; swords and
              heavy armor require <span className="text-accent">Expert</span>. Below the tier you
              cannot equip it at all — though you can still buy it, carry it, and grow into it. The
              market flags anything beyond your reach.
            </p>
            <p className="text-muted">
              This applies to the spoils of a questline too. Finish an early arc and you may be
              handed armor you cannot yet wear: that is the game telling you where a tableau pick
              would go.
            </p>
            <Table
              headers={["Class", "Combat", "Character"]}
              rows={[
                ["Daggers", "Untrained", "Counters and stuns; The Old Tooth trades the plain swing for a riposte and a provoke."],
                ["Axes", "Trained", "Heavy swings and staggering blows, and no fine work at all."],
                ["Swords", "Expert", "Hybrids — riposte, pommel, and blades that guard or mend as they cut."],
                ["Light armor", "Untrained", "Evasion and guile: cautious reads, cloaks, shadow steps."],
                ["Medium armor", "Trained", "Balance: evasions and blocks in the same kit."],
                ["Heavy armor", "Expert", "Absorption: armored guards and perfect blocks."],
              ]}
            />
          </Section>

          {/* CONDITIONS */}
          <Section id="conditions" title="Conditions">
            <p>
              The most dangerous encounters — legendary monsters, the worst of the road — do not
              simply damage you. They leave something behind. There are four serious conditions, and
              each behaves the same way: <strong className="text-dim">1 health per night, every
              night, until it is treated</strong>. With only four health to your name, an untreated
              condition is a countdown.
            </p>
            <p>
              The counter is entirely a matter of preparation. Carry the matching kit and the
              condition is handled overnight, automatically, at no cost — kits are never consumed.
              Carry the wrong kit, or none, and you bleed a point a day until you reach a
              chapterhouse or a market that stocks the cure.
            </p>
            <Table
              headers={["Condition", "Treated by", "Cost"]}
              rows={[
                ["Injured", "Medical Kit", "25 gold — stocked in every market"],
                ["Poisoned", "Mudcap Spores", "15 gold — swamp markets"],
                ["Irradiated", "Shustov Apparatus", "40 gold — plains markets"],
                ["Lattice Sickness", "Siphon Glass", "40 gold — scrub markets"],
              ]}
            />
            <p className="text-muted">
              Specialty cures are scarce and biome-bound, so the kit you want is rarely for sale at
              the moment you need it. Cunning is the other half of the answer: it gives a passive{" "}
              <span className="text-accent">40%</span> chance at Trained and{" "}
              <span className="text-accent">80%</span> at Expert to shrug the condition off before
              it ever lands.
            </p>

            <Sub>Lost</Sub>
            <p>
              A minor condition, and its own small ordeal: getting lost triggers an encounter on the
              road. Bushcraft helps you avoid it; a Cartographer's Kit prevents it outright.
            </p>
          </Section>

          {/* THE ROAD */}
          <Section id="road" title="The Road">
            <p>
              Travel costs spirits. Not by dice — by arithmetic. Every step through hostile country
              and every night camped in the open accrues against you, and the toll is charged as you
              go and accounted for at the end of the journey. Hard country drains you faster than
              easy country.
            </p>
            <Table
              headers={["Hazard", "Accrues", "Removed by", "Untrained", "Trained", "Expert"]}
              rows={[
                ["Thirst", "Each step through scrub", "Waterskin", "1 per 2 steps", "1 per 4", "1 per 8"],
                ["Cold", "Each step through mountains", "Wool Bedroll", "1 per 2 steps", "1 per 4", "1 per 8"],
                ["Fatigue", "Each night camped on the road", "Scarecrow Boots", "1 per night", "1 per 2 nights", "1 per 4"],
              ]}
            />
            <p className="text-muted">
              The right tool removes its hazard entirely for as long as you carry it — and costs you
              a pack slot for as long as you carry it. Bushcraft halves every cost at Trained and
              quarters it at Expert. Nights in a settlement accrue nothing.
            </p>
            <p>
              Spirits do not come back on the road. There is no daily recovery, no slow drift back
              toward full; the only refills are an inn bed or a Recover in a fight. A long circuit
              through the mountains is a bill you pay at the next town.
            </p>
            <p className="text-muted">
              Encounters find you every seven to eleven moves. Some are conversations. Some are
              monsters.
            </p>
          </Section>

          {/* END OF DAY */}
          <Section id="endofday" title="End of Day">
            <p>Each night camped in the wilderness resolves in order:</p>
            <ol className="list-decimal list-inside space-y-2 ml-2">
              <li>
                <strong className="text-dim">Eat.</strong> One ration is consumed automatically.
                Trained Bushcraft stretches your supplies to every other night. No food means going
                hungry: <span className="text-accent">-1 spirit</span>.
              </li>
              <li>
                <strong className="text-dim">Medicine.</strong> Every carried kit treats its matching
                condition. Kits are reusable and stay in your pack.
              </li>
              <li>
                <strong className="text-dim">Health.</strong> Any untreated serious condition costs{" "}
                <span className="text-accent">1 health</span>. If you have none, you recover{" "}
                <span className="text-accent">+1 health</span> instead.
              </li>
              <li>
                <strong className="text-dim">Rescue.</strong> At 0 health the Guild collects you.
              </li>
            </ol>
            <p className="text-muted">
              A rescue is not a death. You wake in the chapterhouse whole and rested, keeping your
              skills, your levels, and anything legendary you have earned — but every purchasable
              item in your pack is gone, undelivered contracts with it, and your gold is reset to 50.
            </p>
          </Section>

          {/* SETTLEMENTS */}
          <Section id="settlements" title="Settlements">
            <p>
              Settlements are the other half of the game: the only place spirits come back, the only
              place to spend gold, and free of the road's accrual. What is on offer depends on
              size — camps, outposts, villages, towns, and cities.
            </p>

            <Sub>Market</Sub>
            <p>
              Sell at <span className="text-accent">50%</span> of buy price; prices jitter slightly
              from town to town.
            </p>
            <Table
              headers={["Category", "Notes"]}
              rows={[
                ["Medical kits", "Always stocked everywhere, plentiful."],
                ["Weapons & armor", "One of a kind. Stock never replenishes once bought — if you leave it, it may still be there; if someone's tastes match yours, it won't."],
                ["Tools", "Limited, biome-flavoured, and they never restock either."],
                ["Specialty medicine", "Scarce and regional. Restocks slowly."],
                ["Rations", "3 gold each. \"Restock food and leave\" fills every empty pack slot you can afford."],
              ]}
            />
            <p className="text-muted">
              Bigger settlements carry more. Remote outposts are the odd exception: they are the
              likeliest place to find specialty medicine, a reward for making the trek.
            </p>

            <Sub>Inn</Sub>
            <p>Three tiers, one night each. Spirits only — health comes back on its own overnight.</p>
            <Table
              headers={["Service", "Cost", "Spirits"]}
              rows={[
                ["A bed for the night", "5 gold", "+5"],
                ["Bed and hot bath", "12 gold", "+10"],
                ["Bed, bath, evening drinks", "25 gold", "Full"],
              ]}
            />
            <p className="text-muted">
              Any stay also applies the medicine you are carrying. Without the right kit, the
              condition stays with you.
            </p>

            <Sub>Storage</Sub>
            <p>
              Every settlement keeps a strongroom of <span className="text-accent">10 slots</span>,
              local to that town. Somewhere to leave the winter bedroll while you cross the scrub.
            </p>

            <Sub>Chapterhouse</Sub>
            <p>
              There is exactly one, in the starting city. Full recovery of health and spirits and
              every condition cleared, free, no medicine required. It is also where a rescue
              deposits you.
            </p>
          </Section>

          {/* CONTRACTS */}
          <Section id="contracts" title="Contracts">
            <p>
              Contracts are your steady income. Accept one at a settlement, carry it to the named
              destination, and the payout collects itself the moment you walk through the gate.
            </p>
            <div className="space-y-2">
              <p><strong className="text-dim">Cost:</strong> Free to accept. Free to abandon.</p>
              <p><strong className="text-dim">Storage:</strong> One pack slot each until delivered.</p>
              <p><strong className="text-dim">Payout:</strong> Scales with distance and with how far
                out the destination lies, with a bonus for delivering somewhere you have never been.</p>
              <p><strong className="text-dim">Negotiation:</strong> +20% at Trained, +40% at Expert.</p>
            </div>
            <p className="text-muted">
              Settlements refresh their offers every few days — faster at well-connected hubs than
              at the ends of the map.
            </p>
          </Section>

          {/* Footer */}
          <footer className="border-t border-edge pt-6 mt-12 text-center text-muted">
            <p className="font-hand">Safe travels, merchant.</p>
          </footer>
        </main>
      </div>
    </div>
  );
}
