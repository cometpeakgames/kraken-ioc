# Serializable KrakenIoc (Unity package)

## Workflow to Replace Namespaces:

Using VS Code, Use `Ctrl + Shift + F` _(Search tab on the left):_

Files to include:
`serializable-kraken-ioc/Assets/**/*.cs`

Remove the following:
`.V1`
`.Core`

Replace the following:
`KrakenIoc.Extensions` with `KrakenIoc`
`KrakenIoc.Exceptions` with `KrakenIoc`
`AOFL.KrakenIoc` with `CometPeak.SerializableKrakenIoc`
