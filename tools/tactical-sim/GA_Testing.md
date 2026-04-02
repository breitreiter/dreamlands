GA Testing

I have an existing C# project with a Monte Carlo harness that evaluates deck quality for a card game. The harness takes a 15-card deck (drawn from a pool of 15 card definitions) and simulates N playthroughs, returning an average "fun score."

I need you to build a genetic algorithm on top of this existing harness to find the highest-scoring encounter decks. Here's the spec:

**Genome:** A deck is an array of 15 card IDs. Duplicates are allowed (a deck can contain multiple copies of the same card definition). Card IDs range from 0 to 14.

**Fitness evaluation:** Use the existing simulation harness to evaluate each deck. Run enough simulations per deck to get a stable average (start with 30, we can tune later). **Run fitness evaluations concurrently using Parallel.ForEach or Task.WhenAll to saturate available cores.** Each deck evaluation is independent and the harness should be thread-safe or instantiable per-thread — check the existing code and adapt accordingly. If the harness has shared mutable state, create per-thread instances.

**GA parameters (make all of these configurable, with these defaults):**
- Population size: 500
- Generations: 200
- Tournament selection, tournament size: 5
- Uniform crossover, 50% per gene
- Mutation rate: 10% per gene (swap to a random card ID from the pool)
- Elitism: preserve top 5% each generation

**Output per generation (log to console):**
- Generation number
- Best fitness, average fitness, worst fitness in population
- The best deck composition (card IDs and counts, not raw array — e.g., "3x Card_02, 2x Card_07, ..." )
- Population diversity metric: number of unique deck compositions in the population

**End-of-run output:**
- Top 20 decks by fitness, deduplicated, with scores
- Card frequency analysis: for each card ID, how often it appears across the top 10% of final population. This tells us which cards are carrying the fun and which are dead weight.
- Convergence generation: the generation where the best fitness stopped improving by more than 0.1% per generation

**Important implementation notes:**
- Sort cards within each deck representation so that [A,A,B] and [B,A,A] are treated as the same deck. This prevents the GA from wasting diversity on equivalent permutations.
- Use a Random instance per thread to avoid contention on System.Random.
- Look at the existing codebase first to understand how the harness is invoked, what the card definitions look like, and what the scoring API returns. Wire into that rather than stubbing anything out.
- Don't normalize or clip the fitness score — let the raw harness output drive selection. We want to see the actual value range.

Start by examining the project structure and the existing Monte Carlo code, then implement.

