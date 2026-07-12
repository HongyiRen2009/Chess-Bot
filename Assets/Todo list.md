~~Remove the generate combined bitboards and simply modify the universal bitboards every time a regular bitboard is modified~~

~~Remove recomputing hashes and implement that with the combined bitboards~~


~~Remove recomputing material and phase in evaluation and instead store that in board~~
Store moves from previous searches and only throw them out if they've been changed. For example, knight moves can only be changed if one of its moves got blocked or it got pinned, otherwise the previous moves are perfectly valid.
Add checks to quiesence search

