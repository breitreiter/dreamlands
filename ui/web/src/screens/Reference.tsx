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
  { id: "tactical", label: "Tactical Encounters" },
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
            <p>You are a travelling merchant exploring the Dreamlands. Your survival depends on managing three core resources.</p>
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
              <Stat label="Health" value="4" note="Lost to severe conditions and hazards. Reach 0 and you'll be rescued — but lose your gold and inventory." />
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
                ["Combat", "Fighting — close encounters, hostile creatures, bandits", "Weapon bonus"],
                ["Negotiation", "Persuasion, deception, social cunning, bartering", "Tool bonus"],
                ["Bushcraft", "Wilderness survival, navigation, travel, foraging", "Tool bonus"],
                ["Cunning", "Trickery, awareness, staying one step ahead, traps", "Armor bonus"],
                ["Luck", "A nudge on the odds — passive reroll chance on failures", "None (passive)"],
                ["Mercantile", "An eye for value — improves contract payouts", "None (passive)"],
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
                ["Weapon", "Combat bonus (+1 to +5)", "Bodkin, Hatchet, Falchion, Broadaxe, Scimitar"],
                ["Armor", "Cunning bonus + injury/freezing resist", "Tunic, Leather, Gambeson, Brigandine"],
                ["Boots", "Exhaustion resist (+1 to +5)", "Fine Boots, Heavy Work Boots, Trail Boots"],
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
                ["Boots", "Pack", "Higher tier = more exhaustion resistance"],
                ["Tools", "Pack", "Grant skill bonuses or condition resistance. Only unique items count — duplicates don't stack."],
                ["Food", "Haversack", "Protein, grain, sweets. Auto-consumed at end of day. 3 gold each at market."],
                ["Medicine", "Haversack", "Cures for specific conditions. Auto-consumed when needed at end of day."],
                ["Tokens", "Haversack", "Minor gear granting +1 to a skill. Story items from encounters."],
                ["Contracts", "Pack", "Delivery contracts — take up a pack slot until delivered."],
              ]}
            />
          </Section>

          {/* CONDITIONS */}
          <Section id="conditions" title="Conditions">
            <p>
              Conditions are ailments you pick up from the environment or encounters.
              They drain your resources each night until treated.
            </p>

            <h3 className="text-dim font-bold mt-6 mb-2">Minor Conditions</h3>
            <p className="text-muted mb-2">Drain spirits each night. Cleared by reaching a settlement or using the right gear.</p>
            <Table
              headers={["Condition", "Drain", "Source", "Cleared By"]}
              rows={[
                ["Freezing", "-3 spirits/night", "Mountains (failed resist)", "Enter settlement or leave biome"],
                ["Thirsty", "-3 spirits/night", "Scrub (failed resist)", "Enter settlement"],
                ["Exhausted", "-3 spirits/night", "Encounters, overexertion", "Inn stay only"],
                ["Lost", "Triggers an encounter", "Failed navigation", "Complete the encounter or enter settlement"],
              ]}
            />

            <h3 className="text-dim font-bold mt-6 mb-2">Severe Conditions</h3>
            <p className="text-muted mb-2">Cost 1 health per night if untreated. Stack up to 3 times. Each medicine dose reduces stacks by 1.</p>
            <Table
              headers={["Condition", "Medicine", "Cost"]}
              rows={[
                ["Injured", "Bandages", "3 gold"],
                ["Poisoned", "Mudcap Fungus", "15 gold"],
                ["Irradiated", "Shustov Tonic", "40 gold"],
                ["Lattice Sickness", "Siphon Glass", "40 gold"],
              ]}
            />

            <h3 className="text-dim font-bold mt-6 mb-2">Resisting Conditions</h3>
            <p>
              Each night on the road, you roll resist checks against ambient conditions
              (default DC <span className="text-accent">12</span>). Equipment like canteens, sleeping kits, and armor
              add resist bonuses to help you pass.
            </p>
          </Section>

          {/* THE ROAD */}
          <Section id="road" title="The Road">
            <p>
              Travel happens tile by tile across a grid map. Each night on the road, you roll resist checks
              against ambient conditions. Some are biome-specific — freezing in the mountains, thirst in the scrub —
              while exhaustion and getting lost can happen anywhere. Serious conditions like poisoning, injury,
              and irradiation only come from encounters.
            </p>

            <h3 className="text-dim font-bold mt-6 mb-2">Foraging</h3>
            <p>
              Each night on the road, you roll Bushcraft to forage for food. Three separate checks
              determine how much you find:
            </p>
            <Table
              headers={["DC", "Yield"]}
              rows={[
                ["16", "1 food item"],
                ["18", "2 food items"],
                ["20", "3 food items"],
              ]}
            />
            <p className="text-muted">You don't forage in settlements — buy food at the market instead.</p>
          </Section>

          {/* END OF DAY */}
          <Section id="endofday" title="End of Day">
            <p>At the end of each day, several things happen automatically in this order:</p>
            <ol className="list-decimal list-inside space-y-2 ml-2">
              <li>
                <strong className="text-dim">Resist checks</strong> — Roll against biome-specific conditions (freezing, thirsty, etc.).
                Equipment resist bonuses help.
              </li>
              <li>
                <strong className="text-dim">Forage</strong> — On the road, roll Bushcraft to find food (skipped in settlements).
              </li>
              <li>
                <strong className="text-dim">Eat</strong> — Up to 3 food items are consumed from your haversack.
                A <em>balanced meal</em> (1 protein + 1 grain + 1 sweets) grants <span className="text-accent">+1 bonus spirits</span>.
                No food means you go hungry — you won't recover spirits from resting that night.
              </li>
              <li>
                <strong className="text-dim">Medicine</strong> — If you have a matching cure for a severe condition, one dose is consumed automatically, reducing stacks by 1.
              </li>
              <li>
                <strong className="text-dim">Condition drain</strong> — Minor conditions drain <span className="text-accent">3 spirits</span> each.
                Untreated severe conditions drain <span className="text-accent">1 health</span>.
              </li>
              <li>
                <strong className="text-dim">Rescue</strong> — If health reaches 0, you're rescued. You lose your inventory and gold but keep your skills and are placed at the nearest settlement.
              </li>
              <li>
                <strong className="text-dim">Rest</strong> — If you ate and slept, recover <span className="text-accent">+1 spirits</span> (or +2 with a balanced meal).
              </li>
            </ol>
          </Section>

          {/* SETTLEMENTS */}
          <Section id="settlements" title="Settlements">
            <p>
              Settlements are safe havens — no biome threats, no foraging.
              They offer services depending on their size: camps, outposts, villages, towns, and cities.
            </p>

            <h3 className="text-dim font-bold mt-6 mb-2">Market</h3>
            <p>Buy supplies and equipment. Sell items at <span className="text-accent">50%</span> of their buy price. Prices vary slightly between settlements.</p>
            <Table
              headers={["Category", "Notes"]}
              rows={[
                ["Food", "Always stocked, unlimited. 3 gold each."],
                ["Bandages", "Always stocked, unlimited. 3 gold each."],
                ["Equipment", "Weapons, armor, boots — limited stock, never restocks once sold."],
                ["Tools", "Skill and resist bonuses — limited stock."],
                ["Specialty Medicine", "Cures for rare conditions — limited stock, restocks slowly."],
                ["Contracts", "Delivery contracts — free to accept, 1 to 3 available per settlement."],
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
              If you have severe conditions, you must have the matching medicine in your haversack — the inn
              will use it during your stay.
            </p>

            <h3 className="text-dim font-bold mt-6 mb-2">Chapterhouse</h3>
            <p>
              Free full recovery — health, spirits, and <em>all</em> conditions cleared, no medicine required.
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
              <p><strong className="text-dim">Delivery:</strong> Automatic — enter the destination settlement and the payout is collected.</p>
              <p><strong className="text-dim">Discard:</strong> You can drop a contract at any time without penalty.</p>
            </div>
          </Section>

          {/* TACTICAL ENCOUNTERS */}
          <Section id="tactical" title="Tactical Encounters">
            <p>
              Some encounters on the road use a card-based tactical system instead of simple skill checks.
            </p>
            <p>
              You have a deck of <span className="text-accent">15 cards</span> drawn from your skills and equipment.
              Each card costs resources to play (momentum or spirits) and contributes progress, momentum, or cancellation.
              Your weapon archetype shapes your tactical style: daggers favor cancellation,
              axes favor raw aggression, and swords offer a hybrid approach.
              Each skill contributes unique intrinsic cards as you level up.
            </p>

            <h3 className="text-dim font-bold mt-6 mb-2">Progress Cards</h3>
            <p className="text-muted mb-2">Deal damage to overcome the challenge. Your main win condition.</p>
            <Table
              headers={["Card", "Cost", "Effect"]}
              rows={[
                ["Free Jab", "Free", "1 progress"],
                ["Momentum Strike", "1 momentum", "2 progress"],
                ["Heavy Strike", "2 momentum", "4 progress"],
                ["Devastating Blow", "3 momentum", "6 progress"],
                ["Spirit Surge", "1 spirit", "3 progress"],
                ["Desperate Surge", "3 spirits", "6 progress"],
                ["Risky Strike", "1 threat tick", "2 progress"],
                ["Reckless Strike", "2 threat ticks", "4 progress"],
              ]}
            />

            <h3 className="text-dim font-bold mt-6 mb-2">Momentum Cards</h3>
            <p className="text-muted mb-2">Build up momentum to fuel stronger plays.</p>
            <Table
              headers={["Card", "Cost", "Effect"]}
              rows={[
                ["Small Opening", "Free", "1 momentum"],
                ["Opening", "Free", "2 momentum"],
                ["Risky Opening", "1 threat tick", "3 momentum"],
                ["Spirit Opening", "1 spirit", "3 momentum"],
              ]}
            />

            <h3 className="text-dim font-bold mt-6 mb-2">Cancel Cards</h3>
            <p className="text-muted mb-2">Counter enemy threats before they hit you.</p>
            <Table
              headers={["Card", "Cost", "Effect"]}
              rows={[
                ["Momentum Cancel", "3 momentum", "Stop threat"],
                ["Spirit Cancel", "4 spirits", "Stop threat"],
                ["Free Cancel", "Free", "Stop threat"],
              ]}
            />
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
