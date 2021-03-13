# Player events
A list of all player events that occur and can have listeners added.

## PlayerSpawned
Called whenever the player spawns in.
- Provides [PlayerEventArgs](xref:JotunnLib.Events.PlayerEventArgs)
- Registered using [PlayerSpawned](xref:JotunnLib.Managers.EventManager.PlayerSpawned)


## PlayerPiecePlaced
Called whenever the player attempts to place a piece, whether they are successful or not. This may be unsuccessful if the piece is placed in an invalid spot, or the player does not have enough resource to place it.
- Provides [PlayerPlacedPieceEventArgs](xref:JotunnLib.Events.PlayerPlacedPieceEventArgs)
- Registered using [PlayerPlacedPiece](xref:JotunnLib.Managers.EventManager.PlayerPlacedPiece)