# SerilogAnalyzer

Provides currently two analyzers:

## Message Template Verifier
Checks your messageTemplate for correct syntax and emits an error if there's a violation to the templating syntax.

## Exception Usage
Checks that exceptions are passed to the `exception` argument and not as a `propertyValue` with a code fix to correct it.
