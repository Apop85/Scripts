# Finde Fragen und Sätze in einer Geschichte
import re
story='''My father died the day I was born. I don’t "believe" in ghosts or anything, but I think I could adjust to the idea pretty quickly; that’s what it felt like "only hearing" about someone that helped make you. 45 years later, my mother dies too. We have to clean out the old house before we sell it, and while I’m emptying their bedroom, I stub my toe on a piece of trim bumped out from the wall; I push the trim back into place, and a whole piece of the wall moves. By the time I’m done, I’ve probably pulled out a hundred sheets of paper; letters, notes to himself, little stories, and pictures — so many pictures. They all had his name on them, all in the same tight, narrow handwriting. I think I was confused more than anything else, I wasn’t choked up, just sitting there wondering why I’d never seen any of this before. Mom had stopped talking about him more recently, but she was just going at that point. When I was younger, I always heard about how everyone loved him, how good his cooking was, how funny he was. Honestly, once I saw the pictures, I half-expected to see another woman, maybe a whole other family. Funny thing was, they weren’t even of him. As best I could tell, they were of random people, just people he saw on the street, with written notes on the back like, “Man waiting outside restaurant for someone to call him back.” Really benign stuff, actually, but the pictures were beautiful. They all seemed a little worn, dirty. Not the physical pictures, but the images, "the people and places", like they all had been forgotten too. Just why? Why did this happen?'''

# Suche nach Fragen
suchmuster=(r"[a-zA-z ’']+[?]")
suchnach=re.compile(suchmuster)
fragen=suchnach.findall(story)
frage=suchnach.search(story)
print(''.center(50, '▼'))
print('Anzahl Fragen:'.ljust(15), len(fragen))
print('Erste Frage:'.ljust(15), (frage.group()).strip())


# Suche nach Sätzen
suchmuster=(r"\w[a-zA-z ’']+[.!:;]")
suchnach=re.compile(suchmuster)
satz=suchnach.findall(story)
sat=suchnach.search(story)
print(''.center(50, '▼'))
print('Anzahl Sätze:'.ljust(15), len(satz), '+', len(fragen))
print('Erster Satz:'.ljust(15), (sat.group()).strip())


# Suche nach Zitaten
suchmuster=(r'"(.*?)"')
suchnach=re.compile(suchmuster)
zitate=suchnach.findall(story)
zitat=suchnach.search(story)
print(''.center(50, '▼'))
print('Anzahl Zitate:'.ljust(15), len(zitate))
print('Erstes Zitat:'.ljust(15), (zitat.group()).strip())


# Suche nach Wörtern
suchmuster=(r"[a-zA-Z’']+")
suchnach=re.compile(suchmuster)
wort=suchnach.findall(story)
wor=suchnach.search(story)
print(''.center(50, '▼'))
print('Anzahl Wörter:'.ljust(15), len(wort))
print('Erstes Wort:'.ljust(15), (wor.group()).strip())

