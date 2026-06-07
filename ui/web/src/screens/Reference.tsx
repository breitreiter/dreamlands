import { useState } from "react";

const sections = [
  { id: "character", label: "Your Character" },
  { id: "skills", label: "Skills" },
  { id: "checks", label: "Skill Checks" },
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
      <h2 className="font-header text-[28px] text-accent mb-4 border-b border-edge pb-2">{title}</h2>
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
      <div className="text-accent text-[24px] font-bold">{value}</div>
      <div className="text-muted mt-1">{note}</div>
    </div>
  );
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
            <p>You are a travelling merchant exploring the imperial borderlands. Your survival depends on managing three core resources.</p>
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
              <Stat label="Health" value="4" note="Lost to severe conditions and hazards. Reach 0 and you'll be rescued, but lose your gold and inventory." />
              <Stat label="Spirits" value="20" note="Your morale and energy. Drained by hardship, restored by rest and food." />
              <Stat label="Gold" value="50" note="Spent at markets and inns. Earned through contracts, encounters, and selling goods." />
            </div>
          </Section>

          {/* SKILLS */}
          <Section id="skills" title="Skills">
            <p>Six skills govern what you're good at. Each ranges from <span className="text-accent">0</span> to <span className="text-accent">+4</span> and adds directly to your skill check rolls.</p>
            <Table
              headers={["Skill", "Used For", "Equipment Bonus"]}
              rows={[
                ["Combat", "Fighting: close encounters, hostile creatures, bandits", "Weapon bonus"],
                ["Negotiation", "Persuasion, deception, social cunning, bartering", "Tool bonus"],
                ["Bushcraft", "Wilderness survival, navigation, travel, foraging", "Tool bonus"],
                ["Cunning", "Trickery, awareness, staying one step ahead, traps", "Armor bonus"],
                ["Luck", "A nudge on the odds: passive reroll chance on failures", "None (passive)"],
                ["Mercantile", "An eye for value: improves contract payouts", "None (passive)"],
              ]}
            />
            <p className="text-muted">
              <strong className="text-dim">Luck</strong> doesn't add to rolls directly. Instead, each level gives a 5% chance (up to 20% at level 4) to automatically reroll a failed check.
            </p>
            <p className="text-muted">
              <strong className="text-dim">Mercantile</strong> increases contract delivery payouts by 10% per level.
            </p>
          </Section>

          {/* SKILL CHECKS */}
          <Section id="checks" title="Skill Checks">
            <p>
              When you attempt something risky, the game rolls a <span className="text-accent">d20</span>, adds your
              skill level and equipment bonuses, and compares the total against a difficulty target.
            </p>
            <p className="font-bold text-dim">d20 + skill + equipment &ge; DC = success</p>

            <Table
              headers={["Difficulty", "DC"]}
              rows={[
                ["Trivial", "5"],
                ["Easy", "8"],
                ["Medium", "12"],
                ["Hard", "15"],
                ["Very Hard", "18"],
                ["Epic", "22"],
              ]}
            />

            <div className="space-y-2 mt-4">
              <p><strong className="text-dim">Natural 1:</strong> Always fails, regardless of bonuses.</p>
              <p><strong className="text-dim">Natural 20:</strong> Always succeeds, regardless of difficulty.</p>
            </div>
          </Section>

          {/* EQUIPMENT & INVENTORY */}
          <Section id="inventory" title="Equipment & Inventory">
            <p>You carry gear in two containers and have three equipment slots.</p>

            <h3 className="text-dim font-bold mt-6 mb-2">Equipment Slots</h3>
            <Table
              headers={["Slot", "Grants", "Examples"]}
              rows={[
                ["Weapon", "Combat moves", "Bodkin, Hatchet, Falchion, Broadaxe, Scimitar"],
                ["Armor", "Defensive combat moves", "Tunic, Leather, Gambeson, Brigandine"],
              ]}
            />

            <h3 className="text-dim font-bold mt-6 mb-2">Containers</h3>
            <Table
              headers={["Container", "Starting Slots", "Holds"]}
              rows={[
                [<strong key="p">Pack</strong>, "3 (upgradeable)", "Weapons, armor, boots, tools, trade goods. Equippable gear and bulky items."],
                [<strong key="h">Haversack</strong>, "20 (fixed)", "Food, medicine, tokens, consumables. Small items that get used automatically."],
              ]}
            />

            <h3 className="text-dim font-bold mt-6 mb-2">Item Categories</h3>
            <Table
              headers={["Type", "Container", "Notes"]}
              rows={[
                ["Weapons", "Pack", "Three archetypes: daggers (exploiting openings), axes (aggressive), swords (hybrid)"],
                ["Armor", "Pack", "Light (cunning-focused), medium (balanced), heavy (injury resist)"],
                ["Tools", "Pack", "Spare you travel hazards (waterskin, bedroll) or cure conditions. Reusable while carried."],
                ["Food", "Haversack", "Protein, grain, sweets. Auto-consumed at end of day. 3 gold each at market."],
                ["Medicine", "Haversack", "Cures for specific conditions. Auto-consumed when needed at end of day."],
                ["Tokens", "Haversack", "Minor gear granting +1 to a skill. Story items from encounters."],
                ["Contracts", "Pack", "Delivery contracts: take up a pack slot until delivered."],
              ]}
            />
          </Section>

          {/* CONDITIONS */}
          <Section id="conditions" title="Conditions">
            <p>
              Conditions are ailments you pick up from encounters. Severe conditions
              cost health every night until treated.
            </p>

            <h3 className="text-dim font-bold mt-6 mb-2">Severe Conditions</h3>
            <p className="text-muted mb-2">
              Cost 1 health per night if untreated. Carrying the matching medicine kit treats
              them automatically overnight; kits are reusable and never consumed.
            </p>
            <Table
              headers={["Condition", "Medicine", "Cost"]}
              rows={[
                ["Injured", "Medical Kit", "25 gold"],
                ["Poisoned", "Mudcap Spores", "15 gold"],
                ["Irradiated", "Shustov Apparatus", "40 gold"],
                ["Lattice Sickness", "Siphon Glass", "40 gold"],
              ]}
            />

            <h3 className="text-dim font-bold mt-6 mb-2">Lost</h3>
            <p>
              Failed navigation can leave you lost. Being lost triggers an encounter on
              the road; finding your way back is its own small ordeal. Bushcraft helps
              you avoid getting lost in the first place; a Cartographer's Kit prevents it.
            </p>

            <h3 className="text-dim font-bold mt-6 mb-2">Resisting Conditions</h3>
            <p>
              When an encounter inflicts a severe condition, Cunning gives a passive
              chance to shrug it off: <span className="text-accent">40%</span> at Trained,{" "}
              <span className="text-accent">80%</span> at Expert.
            </p>
          </Section>

          {/* THE ROAD */}
          <Section id="road" title="The Road">
            <p>
              Travel happens tile by tile across a grid map. The road itself wears you
              down: harsh terrain and nights camped in the open cost spirits as you go,
              and you get an account of the toll at the end of every journey.
            </p>

            <h3 className="text-dim font-bold mt-6 mb-2">Travel Hazards</h3>
            <p className="text-muted mb-2">
              Costs are steady and predictable, not rolled. The right gear removes a
              hazard entirely; Bushcraft halves all hazard costs at Trained and quarters
              them at Expert.
            </p>
            <Table
              headers={["Hazard", "Source", "Spared By"]}
              rows={[
                ["Thirst", "Walking the scrub", "Waterskin"],
                ["Cold", "Crossing the mountains", "Wool Bedroll"],
                ["Fatigue", "Each night camped on the road", "Scarecrow Boots"],
              ]}
            />
            <p className="text-muted">
              Settlement nights are free: sleeping in town costs no fatigue.
            </p>
          </Section>

          {/* END OF DAY */}
          <Section id="endofday" title="End of Day">
            <p>At the end of each day on the road, in order:</p>
            <ol className="list-decimal list-inside space-y-2 ml-2">
              <li>
                <strong className="text-dim">Eat:</strong> One ration is consumed automatically.
                Trained Bushcraft stretches supplies: you only eat every other night.
                No food means going hungry, <span className="text-accent">-1 spirit</span>.
              </li>
              <li>
                <strong className="text-dim">Medicine:</strong> A matching medicine kit treats one severe
                condition overnight. Kits are reusable and never consumed.
              </li>
              <li>
                <strong className="text-dim">Condition drain:</strong> Any untreated severe condition
                costs <span className="text-accent">1 health</span>. With none, you recover{" "}
                <span className="text-accent">+1 health</span>.
              </li>
              <li>
                <strong className="text-dim">Rescue:</strong> If health reaches 0, you're rescued. You lose
                items and gold but keep your skills.
              </li>
            </ol>
            <p className="text-muted mt-2">
              Spirits never regenerate on the road; refill them at an inn.
            </p>
          </Section>

          {/* SETTLEMENTS */}
          <Section id="settlements" title="Settlements">
            <p>
              Settlements are safe havens: nights in town cost nothing, and the road's
              travails reset at the gate.
              They offer services depending on their size: camps, outposts, villages, towns, and cities.
            </p>

            <h3 className="text-dim font-bold mt-6 mb-2">Market</h3>
            <p>Buy supplies and equipment. Sell items at <span className="text-accent">50%</span> of their buy price. Prices vary slightly between settlements.</p>
            <Table
              headers={["Category", "Notes"]}
              rows={[
                ["Food", "Always stocked, unlimited. 3 gold each."],
                ["Bandages", "Always stocked, unlimited. 3 gold each."],
                ["Equipment", "Weapons, armor, boots. Limited stock, never restocks once sold."],
                ["Tools", "Skill and resist bonuses. Limited stock."],
                ["Specialty Medicine", "Cures for rare conditions. Limited stock, restocks slowly."],
                ["Contracts", "Delivery contracts. Free to accept, 1 to 3 available per settlement."],
              ]}
            />

            <h3 className="text-dim font-bold mt-6 mb-2">Inn</h3>
            <p>
              Full recovery of health and spirits, plus clears exhaustion and settlement-cleared conditions.
              Costs <span className="text-accent">9 gold per night</span>. The innkeeper calculates your stay as:
            </p>
            <p className="font-bold text-dim ml-2">
              Nights = max(health deficit, &lceil;spirits deficit &divide; 2&rceil;), minimum 1
            </p>
            <p className="text-muted">
              For example, if you're missing 2 health and 8 spirits, that's max(2, 4) = 4 nights = 36 gold.
              Time advances by the number of nights stayed.
            </p>
            <p className="text-muted">
              If you have severe conditions, you must have the matching medicine in your pack; the inn
              will use it during your stay.
            </p>

            <h3 className="text-dim font-bold mt-6 mb-2">Chapterhouse</h3>
            <p>
              Free full recovery: health, spirits, and <em>all</em> conditions cleared, no medicine required.
              There is exactly one, in Aldgate.
            </p>
          </Section>

          {/* CONTRACTS */}
          <Section id="contracts" title="Contracts">
            <p>
              Delivery contracts are your primary income. Accept a contract at one settlement,
              carry it to the destination, and collect a payout on arrival.
            </p>
            <div className="space-y-2">
              <p><strong className="text-dim">Cost:</strong> Free to accept.</p>
              <p><strong className="text-dim">Payout:</strong> Gold on delivery, boosted by Mercantile skill (+10% per level).</p>
              <p><strong className="text-dim">Storage:</strong> Each contract takes 1 pack slot until delivered.</p>
              <p><strong className="text-dim">Delivery:</strong> Automatic; enter the destination settlement and the payout is collected.</p>
              <p><strong className="text-dim">Discard:</strong> You can drop a contract at any time without penalty.</p>
            </div>
          </Section>

          {/* Footer */}
          <footer className="border-t border-edge pt-6 mt-12 text-center text-muted">
            <p className="font-hand text-[20px]">Safe travels, merchant.</p>
          </footer>
        </main>
      </div>
    </div>
  );
}
