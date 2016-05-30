# SerilogAnalyzer

Provides currently two analyzers:

## Serilog001: Exception Usage
Checks that exceptions are passed to the `exception` argument and not as a `propertyValue` with a code fix to correct it.

## Serilog002: MessageTemplate verifier
Checks your messageTemplate for correct syntax and emits an error if there's a violation to the templating syntax.

## Serilog003: Property binding verifier
Checks coherence between the MessageTemplate properties and the supplied arguments

## Serilog004: Constant MessageTemplate verifier
Checks that MessageTemplates are constant values which is recommended practice so that events with different data / format arguments can be detected as the same event.

## Serilog005: Unique Property name verifier
Checks that all property names in a MessageTemplates are unique
