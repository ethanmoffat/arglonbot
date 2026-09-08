# Easter & Moon Phase Calculation

How the bot resolves moon phases, why there are two calendars, and how Easter is
computed.

## Two moon calendars

`MessageInfo.MoonCalendar` selects which algorithm resolves a `MoonPhase` rule. Each
value maps to an `IMoonPhaseResolver` implementation, registered in `Program.cs` and
dispatched by `MessageSelector` on the resolver's `Calendar` property.

| Value | Resolver | Basis |
|---|---|---|
| `Astronomical` (default) | `AstronomicalMoonPhaseResolver` | True astronomy via CosineKitty `Astronomy.MoonQuartersAfter` |
| `Ecclesiastical` | `EcclesiasticalMoonPhaseResolver` | The church computus (fixed lunar tables) |

## Why two calendars are needed

The **ecclesiastical** rule (the church computus) deliberately ignores real astronomy:

- the vernal equinox is **fixed at March 21**, regardless of the true equinox, and
- the moon is a **tabular / "mean" moon** from fixed lunar tables, not the real Moon.

This was designed for reproducibility: any cleric could compute Easter with arithmetic
alone, no telescope or almanac required, and everyone would agree.

The **astronomical** approach uses the true equinox and the true full-moon instant.
Because "mean" and "true" can land on different calendar days -- and occasionally on
opposite sides of a Sunday -- the two definitions of Easter can diverge by up to a week.
**1981** is the classic modern example: ecclesiastical Easter was April 19, but an
astronomy-based rule yields April 26.

That divergence is exactly why the ecclesiastical calendar exists in this codebase.
Easter is configured with `"MoonCalendar": "Ecclesiastical"` so it matches the real
Western holiday in every year, including divergence years.

## Terminology

- **Ecclesiastical** -- "of the church"; defined by church rule rather than physical
  reality. An *ecclesiastical full moon* is the 14th day of a tabular lunation, not a
  full moon you could photograph.
- **Paschal** -- "of Easter" (from Hebrew *Pesach*). The *paschal* full moon is the one
  that determines Easter: the first ecclesiastical full moon on or after March 21.

Combining both qualifiers, the rule reads:

> Easter is the first **Sunday** after the **paschal ecclesiastical** full moon.

If the paschal full moon falls on a Sunday, Easter is the *following* Sunday.

## How `EcclesiasticalMoonPhaseResolver` works

`Resolve` returns the **paschal full moon** -- a genuine moon-phase date -- and lets
`MessageSelector`'s existing "first Sunday after" logic reconstruct Easter. This is the
same code path the astronomical resolver uses; there is no Easter-specific logic in
`MessageSelector`.

The paschal full moon is computed with the epact method (Reingold & Dershowitz,
*Calendrical Calculations*):

```text
metonicPosition = year mod 19          // position in the 19-year Metonic cycle
century         = (year / 100) + 1
shiftedEpact    = (14 + 11*metonicPosition - (3*century)/4 + (5 + 8*century)/25) mod 30
adjustedEpact   = shiftedEpact + 1  when shiftedEpact == 0,
                                     or shiftedEpact == 1 and metonicPosition > 10
paschalFullMoon = April 19 - adjustedEpact
```

The `(3*century)/4` and `(5 + 8*century)/25` terms are the Gregorian *solar* and
*lunar* equations -- the 1582 reform corrections for skipped leap days and for lunar
drift (~1 day per 300 years).

### Scope and validation

The church's tables only meaningfully define the **paschal** lunation, so the resolver
is deliberately scoped to it:

- Any phase other than `FullMoon` throws `ArgumentException`. The tables define a new
  moon (day 1) and full moon (day 14); first/third quarter have no ecclesiastical
  meaning.
- The rule's window must be able to contain the paschal full moon, which always falls
  between **March 21 and April 18**. `PaschalWindowValidator` enforces this and rejects
  windows that start too late, end too early, span multiple years, or are inverted.

`PaschalWindowValidator` is injected into both the resolver (runtime guard) and
`MoonPhaseRuleValidator` (startup configuration validation), so the rules live in one
place. See "Error handling" below.

## Configuration

```json
{
  "DateStart": "March 21",
  "DateEnd": "April 25",
  "After": {
    "Type": "MoonPhase",
    "Value": "FullMoon",
    "DayOfWeek": "Sunday",
    "WeekOfInterval": "First",
    "MoonCalendar": "Ecclesiastical"
  },
  "Message": "Happy Easter!"
}
```

`DateStart` must be March 21 (the fixed ecclesiastical equinox). An earlier revision
used March 22, which silently skipped the paschal full moon in years where it fell on
March 21 -- notably **2008**, where real Easter was March 23 but the rule matched
nothing and would have fired on April 27 instead.

## Error handling

A misconfigured ecclesiastical rule is caught in two layers:

1. **Startup** -- `MoonPhaseRuleValidator` runs via `AddOptionsWithValidateOnStart`,
   aggregating every invalid rule into one `OptionsValidationException` before the bot
   connects.
2. **Runtime** -- the resolver still throws `ArgumentException` as a guard (config can
   be hot-reloaded through `IOptionsMonitor`). `ArglonBot.PeriodicOpenMouth` catches it
   per channel, logs, and continues, so one bad rule cannot take down the service.

## Known divergence years

Years where an astronomy-based Easter differs from the ecclesiastical (real) date.
From Meeus's published lists -- verify before relying on any single year.

| Year | Note |
|---|---|
| 1981 | Modern historical outlier; covered in tests |
| 2019 | Well-known modern near/divergence case |
| 2038 | Also the latest-possible Easter (April 25) -- double edge case |
| 2049 | Future divergence |
| 2076 | Future divergence |
| 2144 | Future divergence |

## Limitations

- **Western only.** This is the *Gregorian* computus. Eastern Orthodox Easter uses
  Julian-calendar tables and will not match. (Tracked in `docs/todo.md`.)
- **Proleptic before 1583.** The Gregorian calendar did not exist before October 1582;
  earlier years compute but are historically meaningless.

## Test coverage

| Test | Covers |
|---|---|
| `EcclesiasticalMoonPhaseResolverTest` | Paschal full moon values, Easter reconstruction (incl. 1981, 2008, 2038), non-`FullMoon` and invalid-window exceptions |
| `AstronomicalMoonPhaseResolverTest` | True moon-phase instants from CosineKitty |
| `MessageSelectorTest.TestConfiguredEasterRealDates` | End-to-end Easter, 1978-2038 |
| `MessageSelectorTest.TestMoonPhaseDelegatesToMatchingResolver` | Dispatch by `MoonCalendar` (mocked resolvers) |

## Primary sources

Links are provided where a source is freely available online. Print works are marked
N/A -- add a publisher or Google Books link if you find a stable one.

| Source | Use | Link |
|---|---|---|
| U.S. Naval Observatory, "The Date of Easter" | Canonical statement of the ecclesiastical rules | https://aa.usno.navy.mil/faq/easter |
| Explanatory Supplement to the Astronomical Almanac (Urban & Seidelmann) | Rigorous computus + astronomy treatment | N/A (print) |
| Jean Meeus, *Astronomical Algorithms*, Ch. 8 | Gregorian computus; divergence-year lists | N/A (print) |
| Reingold & Dershowitz, *Calendrical Calculations* | Epact method used here; implements both calendars | N/A (print) |
| Fred Espenak, "Moon Phases Table" | True full-moon instants (UT) for comparison | http://astropixels.com/ephemeris/phasescat/phasescat.html |
| CosineKitty Astronomy Engine | The library backing `AstronomicalMoonPhaseResolver` and `Astronomy.Seasons` | https://github.com/cosinekitty/astronomy |
| Wikipedia, "Date of Easter" | Accessible overview; Anonymous Gregorian algorithm | https://en.wikipedia.org/wiki/Date_of_Easter |
