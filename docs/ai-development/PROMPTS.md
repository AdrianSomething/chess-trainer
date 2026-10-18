## Development until 0.24.0

**Timeline:** 13.09.26 - 18.10.26

**Information:**

* This documentation is a reconstruction of the development before deployment.
* The prompts are written in German.
* The specific used AI models could not be recreated for every prompt, as codex does not save the used model. Following Models where used GPT-5.6 Terra (?), GPT-6 Astra (mid, high), GPT-6.1 Sol (ultra/max).
* For the first prompt(s) no paying plan was used. Then, Plus was used.

**Procedure:**

* According to the prompts of the dev the AI build/modified the app according to his specifications.
* The AI used over 1000 Tests to determine if the app is running as expected.
* The dev play tested the app and requested new features/modifications.

**Prompts:**

* 13.09. 16:50 (CET): "Schreibe eine direkt ausführbare App (Windows 11) für das erlernen von Chess Openings.
Folgende Features sollen enthalten sein:
* Ein Schachspielbrett mit den typischen Schachfiguren (weiß, scharz) mit allen Schachregeln (Movement der Figuren, Turnwechsel nach jedem Zug, En passant, Rochieren, Schach/Schachmatt/Remi, ...)
* Ein Ordner in welchem man seine Schachopenings einfügen kann, welche man erlernen möchte (+ Anleitung, wie diese Datei aussehen muss, für AI verständlich) -> entwerfe eine gute Datenstruktur zum speichern von Spielbäumen (sodass man eben diese Openings gut speichern kann)
* Eine gute, Open-Source schon existierende KI, welche, wenn man sich außerhalb des Spielbaumes befindet, eigene gute Züge treffen kann. Sie soll lokal laufen können (ich besitze einen 4060 Laptop mit 8GB dediziert + insgesamt 16GB RAM). Wenn man sich innerhalb des Spielbaumes befindet, soll die KI einen random Zug aus dem bestehenden Spielbaum nehmen.
* Eine transparent erscheinende Evaluierungsskala vom aktuellem Spielbrett laut derselben KI.
* Simples Design -> es geht um Funktionalität und um eine Umgebung in welcher man gut lernen kann. Das Spielbrett selber soll ähnlich aussehen zum Standardspielbrett von Chess.com.
* Speicher für wie gut man schon die einzelnen Züge des jeweiligen Openings kann.
So soll der Benutzer mit der Applikation interagieren können:
* Er denkt sich selber aus, welchen Opening er studieren möchte, schnappt sich die Informationsdatei aus diesem Projekt, welches du erstellen sollst und gibt sie einer externen KI, welche aus dem Internet infos zu diesem Opening sammelt und dazu die Informationen in besagter Datei sammeln kann.
* Der User setzt die Datei in den Ordner für die zu erlernenden Openings.
* Der User öffnet die Applikation und es erscheint eine Anzeige mit der er auswählen kann, welche der Openings, die er eingefügt hat er lernen möchte (wird automatisch aus dem Inputordner extrahiert). Unter jedem Opening steht auch eine vierteilige Progress-bar mit: "Gelernt", "Zu Wiederholen", "Angeschaut", "Unbekannt" -> wenn ein Zug eines Openings als "Gelernt" gilt, dann geht er über Zeit in die "Zu Wiederholen" Kategorie.
* Nach der Auswahl, erscheint direkt das Spielbrett und man lernt das ausgewählte Opening. Dabei wird die aktuelle Stellung die man lernt angezeigt und der User muss jetzt selber den Zug auswählen, welchen er machen würde.
* Nach der Zugwahl bekommt der Benutzer Feedback, ob es der richtige Zug war. Wenn falsch, darf der Benutzer selber aussuchen, ob er weiterprobieren möchte, oder den Zug angezeigt bekommen möchte (das weiterprobieren, hat aber keine Auswirkung auf den Status des Lernprogress des jeweiligen Zuges). Bei der Anzeige des richtigen Zuges (entweder durch selbstständigem Gelingen, oder durch anzeigen lassen), wird die Evaluierung laut KI angezeigt und der Benutzer hat selber die Wahl, ob er gegen die integrierte KI weiterspielen möchte ("Weiterspielmodus"), nach dem oben benannten Schema (falls Spielbaum vorhanden, dann zufälliger Zug daraus (Regenerate Button auch einfügen, immer da wo mehrere Möglichkeiten), sonst nach eigener KI Evaluierung) (Info, ob man sich außerhalb des Spielbaumes befindet einfügen) oder ob er den nächsten Zug wiederholen möchte (das geht, dann immer so weiter, bis der Benutzer den ganzen Baum auswendig kennt).
* Füge auch einen "Freeplay-Modus" hinzu, welcher praktisch, wie der "Weiterspielmodus" beim Lernen fungiert nach dem Opening-Speilbaum, nur dass, wenn man sich außerhalb des Spielbaums bewegt hat, weil man die falsche Wahl getroffen hat, man zurückgesetzt wird zur vorherigen Stellung (Nullstellungsbutton auch einfügen). Falls der Spielbaum irgendwann nicht mehr weitergeht, soll man das angezeigt bekommen, aber man kann weiterspielen.
Weiteres:
* Jedes Opening ist strickt dadurch definiert aus welcher Perspektive man lernt. Bspw. besteht eine Datei daraus, dass man die Pirc Defence aus der Perspektive von Schwarz lernt. -> Während weiß verschiedene Antworten auf die Züge von Schwarz hat, gibt es für Schwarz nur eine richtige Lösung (auch, wenn es in der Realität auch Variationen geben würde).
* Ein Zug eines Opening gilt nur als "Gelernt", wenn 3-mal hintereinander der richtige Zug ausgewählt wurde (nach Intervallen von Zeit/Zügen). Beim "Zu Wiederholen"-Status von Zügen reicht ein einmaliges Gelingen, um zum Status "Gelernt" wiederzukommen, beim nicht Gelingen rutscht er wieder in den Status "Angeschaut"."
* 13.09. 18:51: "Die von dir bereitgestellte App ist nicht ausführbar. Die Ereignisanzeige liefert folgende Fehlermeldung:
Fehler
Application: ChessOpeningTrainer.exe
CoreCLR Version: 10.0.1226.42308
.NET Version: 10.0.12
Description: The process was terminated due to an unhandled exception.
Exception Info: System.ArgumentException: Das Steuerelement unterstützt keine transparenten Hintergrundfarben.
at System.Windows.Forms.Control.set\_BackColor(Color value)
at ChessOpeningTrainer.MainForm..ctor()
at ChessOpeningTrainer.Program.Main()
Fehler
Fehlerhafter Anwendungsname: ChessOpeningTrainer.exe, Version: 1.0.0.0, Zeitstempel: 0x6a89b77a
Fehlerhafter Modulname: KERNELBASE.dll, Version: 10.0.26100.9444, Zeitstempel: 0x28606b21
Ausnahmecode: 0xe0434352
Fehleroffset: 0x00000000000c41ca
Fehlerhafte Prozess-ID: 0x3F98
Fehlerhafte Anwendungsstartzeit: 0x1DD43935741CBA5
Fehlerhafter Anwendungspfad: [local application path redacted]
Fehlerhafter Modulpfad: C:\\WINDOWS\\System32\\KERNELBASE.dll
Berichts-ID: [crash-report identifier redacted]
Vollständiger Name des fehlerhaften Pakets:
Fehlerhafte paketbezogene Anwendungs-ID:
Information
Fehlerbucket 1428331470712939826, Typ 4
Ereignisname: APPCRASH
Antwort: Nicht verfügbar
CAB-Datei-ID: 0
Problemsignatur:
P1: ChessOpeningTrainer.exe
P2: 1.0.0.0
P3: 6a89b77a
P4: KERNELBASE.dll
P5: 10.0.26100.9444
P6: 28606b21
P7: e0434352
P8: 00000000000c41ca
P9:
P10:
Angefügte Dateien:
[local crash-report path redacted]
[local crash-report path redacted]
[local crash-report path redacted]
[local crash-report path redacted]
[local crash-report path redacted]
Diese Dateien befinden sich möglicherweise hier:
[local crash-report path redacted]
Analysesymbol:
Es wird erneut nach einer Lösung gesucht: 0
Berichts-ID: [crash-report identifier redacted]
Berichtstatus: 268435456
Bucket mit Hash: f9b4cb86595eba75c3d273e7ad5b2532
CAB-Datei-Guid: 0"
* 13.09. 19:42: "Die Applikation funktioniert. Verändere sie nun wie folgt:
* In deinem Beispiel (Pirc Defence) gibt es viel zu wenig Züge (-> man kann nicht gut testen) -> guck dir im Internet Datenbanken an für Pirc Defence, was die besten Züge sind für Schwarz und die häufigsten Antworten von Weiß (wenn möglich) und integriere diese in der JSON Datei mit einer sehr hohen Tiefe (mind 100 Züge zu lernen)
* Im Spielbrett existiert gerade noch keine richtige Deselection, wenn man mit der linken Maustaste auf dem Spielfeld daneben klickt, dann verbleiben diese grünen Punkte von der zuletzt ausgewählten Figur (diese Zeigen ja an, wo diese Figur hingehen soll) -> die grünen Punkte sollten auch verschwinden
* Ich möchte mit Rechtsklick Pfelder Markeiren können (dann werden sie leicht orange), und wenn ich drag, dann wird ein Pfeil vom Startfeld zum Endfeld gezeichnet auch dem Spielbrett (spezifisch für Pferdbewegung wird entsprechend ein L als Pfeil gezeichnet und nicht nur eine diagonale Linie).
* Die Spielfiguren sollten zur Zielposition per drag and drop verschiebt werden können (bisher muss man ja immer auswählen über zwei Mausklicks)
* Die Sprites der Figuren sollten simpler sein, für die Lesbarkeit, gerade ist jede Figur noch sehr detailliert.
* Gerade erscheint, wenn Schach gegeben wurde direkt eine Anzeige, dass man Neustarten muss. Bei Schach ist aber nach einem simplen Schach das Spiel noch längst nicht zu Ende. Es sollten entsprechend nur die legalen Züge möglich sein.
* Die Engine ist gerade glaube ich noch sehr schwach. (Man kann ja eine light und eine heavy Version machen, aber mein Laptop packt auf jeden Fall noch Größenordnungen mehr). Lass auch optional bei freiem Spiel anzeigen, welcher der nächste beste/zweitbeste/drittbeste Zug ist für einen selber, wenn man außerhalb des Openings gespielt hat.
* Bei der Evaluierungsanzeige bewegt sich gerade nur ein oranganer Stich. Am besten bewegt sich der weiße und schwarze Teil mit (sodass man praktisch ein Verhältnis zwischen schwatz/weiß hat).
* Mach eine Anzeige für die bisher gespielten Züge in Schachnotation, sodass man sich beim Lernen orientieren kann. -> man soll auch durch diese Anzeige auch zu den einzelnen Züge gehen können, um einfacher verstehen zu können, wie der Zug nochmal zu Stande kam.
* Baue einen Knopf ein, um seinen Lernfortschritt für das jeweilige Opening zu reseten.
* Oft steht am Ende, nachdem man eine Variante gelernt hat im Lernmodus, dass man die Option hat Weiterzuspielen, obwohl man noch nicht gelernte Stellungen hat -> gehe doch dann zur nächsten Stellung weiter, statt dass man nach jeder durchgespielten Variante immer wieder zum Anfang muss."
* 14.09. 2:02 "Ändere folgendes:
* Allgemein:

  * Es gibt einen Delay, für wann man erst die nächste Figur auswählen darf, obwohl der Zug vor einem schon längst gespielt wurde (\~1s). Dieser sollte entfernt werden.
  * Die Figuren sind jetzt zu minimalistisch gehalten (ja nur noch Buchstaben bzw. Punkte), bitte ändere sie zurück in (simpler) Pikogramm-Form, die eigene Silhouette ausfüllend.
  * Beim Drag and Drop der Figuren, sollte sich die Figur mit dem Mauszeiger bewegen visuell, damit auch klar ist, dass wirklich ein Drag and Drop stattfindet.
  * Ein einmaliger Linksklick auf das Brett, sollte zur kompletten Löschung aller Markierungen führen.
  * Die Pfeilspitzen der Markierungen werden Teilweise vom Ende der Linie verdeckt -> Also die Linie nicht auch noch durch die Pfeilspitze ragen lassen.
  * Das Layout sieht ein wenig Arsch aus: z.B. verdeckt der Openings-Button, wer gerade am Zug ist (da steht ja immer, schwer lesbar Schwarz ist am Zug); die Evaluationsbar laut KI, sollte so hoch sein wie das Spielbrett selber und auf der linken Seite.
  * Die Evaluationsbar sieht auch nicht so aus, als würde sie richtig funktionieren, eine objektive Verschlechterung meiner Position (schwarz), sorgt laut KI für eine Erhöhung der Punkte und des Balkenanteils für Schwarz.
* Lernmodus:

  * Es gibt ja diesen Fortschritt-Button auch innerhalb des Lernmodus. Der sollte da nicht existieren, mitten im Lernen seinen Fortschritt zurückzusetzen, sollten wir nicht ermöglichen.
  * Nach dem man die richtige Antwort kennt, sollten automatisch die Top 3 Moves der Engine angezeigt werden (mit entsprechenden Folge beste Moves -> mit anklicken zum Anzeigen des Bordes)).
  * Weiterspielen-Button sollte nicht existieren, man sollte einfach direkt weiterspielen können.
  * Lernzug-Button sollte auch mit der Hilfe von Enter oder Leertaste betätigt werden können.
  * Es sollte klarer gemacht werden, wann ein Zug richtig/falsch war, der Text ist zu wenig Feedback.
  * Fortschritt gezählt <-> mit höherem Abstand für nächste Stufe, sollte dort nicht stehen. Immer im Lernmodus, sollen nur Züge gezeigt werden, welche die nächste Lernstufe erhöhen -> fang nicht immer bei der Ausgangsstellung an (v.a. wenn die schon gelernt wurde), sondern bei der nächsten noch ungelernten Stellung.
* Freeplay:

  * Man sollte auch während dem Freeplay die Möglichkeit bekommen nach einem Missversuch, die richtige Lösung für die nächste Stellung anzeigen zu lassen (wie bei Lernmodus).
  * Hier finde ich auch nicht, wie man sich die Top 3 nächsten Moves anzeigen lassen kann.
* neuer Modus: Rätsel:

  * erstellet automatisiert ein Schachrätsel (um sein Mid- bis End-Game zu verbessern)
  * versuche dabei die Schachrätsel, nachdem man ein paar mal diese gemacht hat nicht für den Benutzer repetetiv erscheinen zu lassen
  * wenn du es schaffst kannst du neben Schachmatt Rätsel auch welche integrieren, wo man nur einen capture bekommt, und daraus einen Vorteil bekommt
  * kannst die Rätsel auch gegenchecken lassen, ob es dort wirklich nur eine gute Lösung gibt mit der integrierten KI
  * dabei soll der Benutzer wie folgt interagieren: er drückt auf dem Rätsel Modus (im besten Fall, wurden vorher schon Rätsel erstellt und gespeichert, sodass Ladezeiten minimal sind, und während er selber die Rätsel löst werden auch neue live erstellt); das erste Rätsel erscheint, welches im Normalfall mehrere Züge benötigt, welche auch für eine Engine (wie ich sie habe), eindeutig ist, wenn ein Zug falsch gewesen sein sollte, dann kann man wiederholen (so lange wie man möchte), oder man lässt sich die Lösung für diesen Zug anzeigen; wenn die Zugabfolge gelöst wurde, erscheint die Evaluationsanzeige mit zusätzlich den 3 besten Moves.
  * Dieser Modus, kann ruhig konzeptionell komplizierter sein, weil die Erstellung von einzigartigen, guten und lösbaren Rätseln, wahrscheinlich nicht so einfach ist."
* 14.09. 13:41: "Ändere folgendes:
* Allgemein:

  * Wenn man sich schon auf der aktuellen Stellung befindet (nachdem man sich bspw. nur kurz eine Sache angeschaut hat), sollte man nicht noch den Knopf "Zur aktuellen Stellung" betätigen müssen, der im Endeffekt nichts macht.
  * Die Implementierung des Anzeigen lassens der Enginevarianten ist komisch gelöst:

    * Die Funktion des Kästchens "Top 3 + Varianten anzeigen" sorgt einmalig dafür, dass diese angezeigt werden, aber danach wird irgendwie nur noch die Analyse nochmal neu gemacht, wo sich mir nicht erschließt, was die Funktion davon ist (man kann also nicht das nicht mehr versteckt lassen, sondern es bleibt permanent erhalten)
    * Diese Top 3, sollten für die aktuelle Stellung zwar zwischengespeichert werden, aber wenn man dann bspw. im Lernmodus in den Stellungen zurückgeht, sollte einerseits sich die Evaluation und die Top 3 anzeige entsprechend des angezeigten Spielbrettes verändern, und man sollte selber noch Züge spielen können (auch von der Gegenseite), die nicht in der zu lernenden Theorie stehen, um auszuprobieren, was die Engine dazu sagt. Mit der "Zur aktuellen Stellung"-Taste, sollte man entsprechend wieder zurück zum eigentlich gelerntem.
    * Die Top 3 Anzeige ist nicht gut angezeigt. Bisher ist es ja eine Liste, wo man sich durchklicken kann. Ich würde mir aber wünschen, wenn es wirklich nur 3 Zeilen sind, und man sich innerhlab der Zeile immer weiter die besten Enginemoves durchklicken kann, und dann selber, wenn man sich die Stellung anguckt, selber probieren, was die Engine zu einer eigenen Variante sagt. -> also nur noch 3 Zeilen mit durchklicken innerhalb der Zeile, statt wie bisher innerhalb einer Liste.
  * Füge für wenn man selber die Figur über das Draufklicken bewegt (aber nicht bei Drag and Drop) und wenn die Engine selber spielt oder bei Lernkarten eine \~0,25s Animation einfügen, bei welcher die Figuren verschoben wurden, damit man checkt, was die jetzige Position von der vorherigen Unterscheidet. Bei letzterer (den Lernkarten), wäre es cool, wenn du dir das aktuelle Bord anschaust, mit dem gewolten neuen Bord vergleichst, zur letzten gemeinsamen Stellung/Zug springst und von da an die Animationen der Bewegung der Figuren laufen lässt (werde immer schnelle bis \~0,1s pro Animation).
  * Beim Drag and Drop, sollte auch angezeigt werden, wo sich die Figur hinbewegen kann (also die grünen Punkte sollten auch hier beibehalten werden). Das ist nämlich noch nicht verpflichtend der Fall, das passiert nur, wenn man vorher auf die Figur draufgeklickt hat.
* Rätselmodus:

  * man sollte nicht zur Originalstellung zurückkehren müssen, um sich das nächste Rätsel anzuschauen (ähnlich auch im Lernmodus).
  * Erstelle ein README.md File, um die Funktionalität der Random Generierung verstehen zu können.
  * Versuche weiter die Diversität der Aufgaben zu erhöhen, während du dabei die Akkuratheit der Rätsel beibehältst.
* Neuer Modus: Custom Opnening Generator:

  * Grob: Man soll selber eine kleine Startsequenz auf dem Bord machen (selber kontrolliert man beide Seiten) und dann lässt man mit der Hilfe der integrierten Engine eigene Lernkarten generieren, nachdem man gewisse Voreinstellungen gemacht hat.
  * Voreinstellungen (vom Benutzer selber einstellbar, mit integrierter anzeige, wie viele Karten dadurch geschätzt entstehen werden -> auch anhand der vorher festgelegten Startstellung):

    * min Depth (int = 5): Wie tief die Engine mindestens gehen sollte bis danach gesucht wird, inwieweit sie eine Stellung als zu Komplex bewertet, oder als gut für den Benutzer weiterspielbar.
    * max Depth (int = 10): Wie tief die Engine sich maximal von der Startstellung entfernen darf, bis zwangläufig abgebrochen wird.
    * easy to play Threshold (float = -2): Wenn der Spielzug von der Gegenseite diese oder eine tiefere (oder höhere, muss man da auch gucken, was Sinn macht) Evaluation zur Folge hätte, dann wird diese nicht in die Lernkartenerstellung mit einberechnet. (Theoretisch kann dadurch auch ein vorzeitiges Ende für einen Branch geschehen, bevor minimal Depth erreicht wurde)
    * too complex Threshold (int = 4): Wenn man sich innerhalb von min und max Depth befindet, wird der Branch abgebrochen, wenn die Gegenseite 4 oder mehr Züge spielen könnte.
    * max Positions (int = 200): Wenn so viele Karten zum lernen durch die Engine schon erstellt wurden, dann wird der Vorgang abgebrochen und nur nach zusätzlicher Bestätigung durch den Benutzter, kann weitergemacht werden.
    * Vielleicht fällt dir noch etwas weiteres sinnvolles ein. (Vielleicht auch, dass man einen Verlauf der Thresholds integrieren kann, dass wenn man erst kurz nach min Depth ist, die Threshold anders gestaltet sind als kurz vor Ende)
  * Aufgabe der Engine (bspw. aus der Sicht von Schwarz zum Lernen): Die Engine würde sich also, wenn vor min Depth für die Moves von weiß angucken, welche Moves plausibel sein können (also oberhalb des easy to play Thresholds liegen). Für jeden dieser Moves, wird ein separater Branch erstellt. Für die Züge von Schwarz wird im Branch immer der beste Zug laut Engine genommen und ans Ende des jeweiligen Branches gehängt. Das ganze immer so weiter, bis wenn man innerhalb vom Intervall \[min Depth; max Depth] angekommen ist, wo noch zusätzlich too complex als Check kommt, bzw. max Depth generell als Hardcutoff gilt.
  * Achtung: Vergiss nicht, dass man die Perspektive verändern können muss bei der Voreinstellung, sonst kann man ja nicht auch einen Opening bspw. für schwarz machen, sondern für für weiß.
* Experimentelle Variante der Rätselgenerierung des Rätselmodus (falls nicht so schon implementiert):

  * Engie spielt gegen sich selber mehr oder weniger gute Moves (wählt bspw. zufällig aus, ob es den besten oder den 5. besten oder den 2. besten, ... auswählt).
  * danach wird immermalwieder gecheckt, ob es nur eine moveabfolge (bzw. Taktik) (zwischen 2- \~6 Züge) gibt, welche klar für eine Seite zum Vorteil sein könnte, wo aber nur eine Abweichung zum Problem werden könnte (signifikanter Spielzug)
  * falls die Engine was findet, wird das als Rätsel benutzt.
  * Achtung: Beschreibung dient nur als grobe Idee, vielleicht fällt dir ja auch noch andere Features dazu ein."
* 14.09. 22:26: "Ändere folgendes:
* Allgemein:

  * Mir ist unklar, warum bei den 3 Enginevarianten teilweise bspw. nur nach einem Zug augehört wird. Eigentlich sollten doch dann die restlichen Paar besten Moves weiter angezeigt werden und nicht plötzlich aufhören (passiert nur bei manchen, bei denen ich keinen Grund zuordnen kann, v.a. weil es nicht daran liegt, dass nicht mehr weitergespielt werden kann, sondern es könnte schon und beim anklicken werden mir dann auch die weiteren Varianten angezeigt, also verstehe ich nicht).
  * Mach den Slider unter den Enginevarianten (, wenn es mehr Einträge sind, als auf dem Bildschirm platz ist) dunkler, sodass es mit dem Rest passt.
  * Füge im Menü, wo man die Openings sieht die Möglichkeit ein das jeweilige Opening zu löschen (soll nicht möglich sein, bei deinem default Opening).
  * Füge leichte/angenehme Soundeffekte ein mit einer Toggelfunktion für Mute.
* Custom Opening Generator:

  * Bei der Erstellung der Grundstellung, sollte die Evaluationsanzeige dargestellt werden:
  * Die Benutzung hatte schonmal funktioniert, aber ich habe auch einmal folgende Fehlermeldung bekommen: "Generierung fehlgeschlagen: Die gewünschte Engine Suchtiefe wurde nicht für alle Antworten erreicht." -> wenn der Grund Laufzeit war, dann in Ordnung (müsste irgendwo transparent stehen), wenn es aber einen anderen Grund gibt, dann bitte Fixen (z.B. bei vorzeitigem Schachmatt, dann muss der ja ignoriert werden), denn die Erhöhung der Engine Suchtiefe, sollte eigentlich nur die Qualität der Ergebnisse verbessern (und entsprechend eine längere Verarbeitungszeit voraussetzen).
  * Während der Generierung gucken, wo sich einzelne Branches wieder zusammenfügen lassen (Kompression), weil sie in die gleiche Stellung wieder übergehen -> füge auch eine weitere Variable "max Branch Compression Evaluation Loss Acceptance" (float = 0,1) (vielleicht findest du einen besseren Namen), welcher beschreibt wie viel man an Evaluierungsbewertung verlieren darf im Vergleich zum eigentlich besten Zug bei jener Position aus der Sicht der lernenden Seite. Ein Evaluierungsverlust von 0, beschreibt, dass eine Kompression der Branches sowieso passieren würde, weil es sich um den besten Zug in dieser Position handelt. -> Zur smarten Reduktion der Lernkarten
  * Beim erstellen der Lernkarten, sollten automatisch auch Lernkarten von Nullstellung zur Startstellung generiert werden.
  * Ändere entsprechend der neu hinzugefügten/geänderten Parameter auch die Abschätzung der zu generierenden Kartenanzahl.
* Rätselmodus:

  * Sagen wir bei einem Rätsel soll man Matt in 4 machen, dann sollten alle Wege akzeptiert werden, welche zu Matt in 4 führen und nicht nur der vorher berechnete. (vorausgesetzt das wurde nicht schon implementiert und ich habe mich nur verguckt)
  * Zeige, zum Debuggen an aus welcher Quelle die Rätsel sind (also ob Selbstspiel oder Komposition).
  * Füge Schwierigkeitsmodi hinzu (nach ELO, ist wahrscheinlich eher schwieriger, wenn das nur mit Unsicherheit geht, dann mach einfach Leicht, Normal, Schwer) (bspw. Rätsel, bei welchem es viele Möglichkeiten gibt Figuren zu bewegen oder höhere Matt in N sind i.d.R schwerer).
* Neue Visualisierung: Branch Modus:

  * Im dem Menü, wo man all seine Openings sieht, soll man auch seinen Opening als Branch darstellen lassen können (Anzeige in 2d mit integrierter Kamerabewegung und Zoomen).
  * Dabei auch nicht vergessen, dass der Branch sich ja nach deiner Implementation auch wieder zusammenfügen kann.
  * Der Branch soll farbig auch den Progress anzeigen lassen können. (also jeder gelernte Knoten in einer anderen Farbe, als die noch nicht gelernten)
  * Im Lernmodus soll man sich auch die Stellung innerhalb des Branchsystem anzeigen lassen können (mit Markierung um Knoten).
  * Wenn man auf einen Knoten klickt, sollte sollte das Spielbrett erscheinen mit der geeigneten Stellung + Evaluierungsleiste von Engine."
* New Branch was generated as at 15.09. 10:59 the prompt had no content.
* 15.09. 12:22: "Ändere folgendes:
* Allgemein:

  * Das Spielen, das Schlagen einer Figur, das Schach und das Schachmatt setzen, sollte eigene Sounds erzeugen.
  * Die Sounds kommen gerade zu spät. (Ihre Attack Time ist wahrscheinlich viel zu lange.)
  * Es gibt keine Spielsounds (nur Bestätigungs oder Ablehungssounds) im Rätselmodus oder wenn man selber Sachen ausprobiert im Freeplaymodus innerhalb der Openings.
  * Mindestens der Figuren-Setzen-Sound hört sich nicht gut an. Ist eher ein 8-Bit-Beep.
  * Die Anzeige zur Auswahl bei der Promotion des Bauerns sieht nicht gut aus, benutze einfach jene Silhouetten, welche schon im Spiel benutzt werden.
* Lernmodus:

  * Gerade spielt ja noch, nach dem richtigen Auswählen der nächsten Spielfigur, die KI direkt danach weiter. Jetzt soll stattdessen alle Möglichkeiten der im Branch auftauchenden Gegner-Konterzüge mit dem Markierungstool dargestellt werden und es taucht ein neuer Button auf "Weiterspielen". Wenn man jetzt Leertaste drückt, dann kommt einfach die nächste Lernkarte (wie zuvor), wenn man aber Enter drückt spielt die Engine weiter (und wählt zwangsläufig entsprechend der aufgeführten Markierungen den nächsten Spielzug und spielt danach wie vor deiner Bearbeitung weiter). Auch, wenn man das Ende des Branches erreicht hat.
  * Falls noch nicht so integriert. Beim ersten mal Lernen gehe immer so tief wie möglich in einem Branch hinein und erst, wenn man das Ende erreicht hat, gehe zur spätesten noch nicht angeschauten Abzweigung, lerne von da wieder so tief wie möglich und wiederhole.
* Custom Opening Generator:

  * Probier mal die Lernkartengenerierung mit mehr benutzten Threads schneller zu machen (selber einstellbar) -> gerade benutze ich max 15%, normal eher so 8% meiner Leistung, während ich teilweise über eine Stunde auf die Generierung warte -> wahrscheinlich weißt du besser, wie man das optimiert kriegt, aber man könnte ja bspw. einen Thread für die Zuordnung im Branch benutzen, während die restlichen die Vorverarbeitung mit den Engine für jedes Brett leisten
  * Die Schätzung der Lernkarten ist kompret dilusional. Er sagt mir, es werden eine Millionen Karten, es wurde aber nur \~700.
  * Kontrolliere, ob du folgende Sachen schon machst:

    * Du kontrollierst beim zusammenfügen der Branches, ob diese Stellung nicht nur nach derselben Anzahl von Zügen so schon stattgefunden hat, sondern, ob es in der Vergangenheit auch schon einen Zug gab, der dieselbe Stellung hatte.
    * Du kontrollierst auch, ob sich nach einem gegnerischem Zug die Stellung aus Zufall gedoppelt haben (Transposition) -> und nicht, nur, inwieweit man eine Transposition erzwingen kann durch eigene Züge.
  * Versuche generell den Branch so zu optimieren, dass er für das Lernen einfacher ist. Am besten fallen dir dazu noch ein paar Sachen ein, um sich so wenig Entscheidungen wie nur möglich einprägen zu müssen, während dabei das eigentlich gelernte nahezu gleich bleibt. (Mache Teile dieser Vereinfachung auch optional.)
  * Der Tab soll auch im Hintergrund laufen können, sodass man währenddessen auch bspw. Rätsel machen kann, oder andere Openings lernen kann.
  * Speichere für jeden Knoten der abgebrochen wurde, weil er entweder zu Komplex war, oder weil max Depth erreicht wurde alle möglichen sinnvolle Gegner-Konterzüge, sodass sie dann im Lernmodus darstellbar sind. -> Es sollte also immer mit den möglichen Variationen des Gegner ein Branch beendet werden, statt wie jetzt mit dem eigen Move.
  * Füge die Möglichkeit einer mittleren Baumtiefe ein, sodass man die Möglichkeit hat, dass sich im Verlauf der Lernkartengenerierung den Baum selber zu Formen, indem man die Variablen für Abschnitt 1 (z.B. min Depth=5 bis middle Depth=10 -> Eliminierung nur der Komplexesten Branches) anders hat als für Abschnitt 2 (z.B. middle Depth=10 bis max Depth=20 -> Eliminierung der meisten mit Beibehaltung von einfachen klaren Branches)
* Branchsystem:

  * Bisher steht neben jedem Knoten "2. Schwarz", "3. Weiß" und auf jeder Verbindung der benötigte Zug -> lass beides Weg und schreibe nur noch innerhalb der Knoten den benötigten Zug, um vom vorherigen Bord zum Bord zu kommen, welcher der angeschaute Knoten beschreibt.
  * Das ganze Branchsystem an sich sieht noch relativ unübersichtlich aus. Da die gesamte Höhe so oder so von der Spalte abhängt, welche die meisten Züge hat, kann man doch in den restlichen Spalten die jeweiligen Züge zentriert vor jenen Zügen in der nächsten Spate anzeigen lassen, deren Variation sie zugehörig sind (-> also wenn eine Position 3 Möglichkeiten hat, wie sie vom Gegner gespielt werden kann, dann lass doch diese Position mittig vor den 3 Variation der nächsten Spalte anzeigen und nicht irgendwo oben, wo man sie dann nicht mehr zuordnen kann; ähnlich auch mit den Nachfolgenden, wenn sich die Anzahl der Branches verkleinern, kann man doch, wenn es bspw. nur eine nachfolgende Variation gibt, die Variation in der selben Zeile sein, wie die vorangestellte (-> all das setzt voraus, dass es keine Branchkompression gibt, bei diesen probier die Komprimierten Branches in der vorherigen Spalte so nach wie möglich (am besten benachbart) anzeigen zu lassne)), dabei darf auch die maximale Höhe des Branches leicht in die Höhe abweichen, wenn dadurch der Branch übersichtlicher wird. Branches welche Enden, sollten innerhalb eines Bündels von Branches (also stammend aus der selben vorherigen Stellung) nach unten orientiert sein.
  * Füge Variationen ein. Wenn innerhalb einer Position aus unterschiedlichen Zügen des Gegners dieselbe Antwort resultiert. Fasse diese zusammen (im Branch visuell, v.a. wenn danach bei beiden dieselben oder keine weiteren Antworten folgen (also der Branch endet) und innerhalb des Lernmodus (-> also im Lernmodus, sollte diese Funktion auch integriert werden), dass eine Karte erstellt wird, welche die Verschiedenen Variationen anzeigt, aber dieselbe Antwort vom Benutzer erwartet wird).
  * Der Tab soll auch im Hintergrund laufen können, sodass man währenddessen auch bspw. Rätsel machen kann, oder andere Openings lernen kann.
  * Integriere Funktion manche Branches komplett auszuschließen oder auf später zu verschieben, nachdem man das wichtigste zuerst gelernt hat.
  * Integriere einen automatische Filter für "Human Moves" (auch möglich die Integration schon in Custom Opening Generator zu machen) für optionales Ausblenden von Moves, welche erst durch die Evaluierung durch eine Engine mit über bspw. 10 Züge Weitsicht (du lässt entsprechend praktisch diese dümmere Engine das ganze durchlaufen und guckst, ob diese denkt, dass dieser Zug eigentlich für den Gegner einen Nachteil bringt) als gut bewertet werden.
* Rätselmodus:

  * "Prüfe deinen Matt Weg", sollte es nicht geben, berechne alle akzeptierten Wege einfach vor (es sollten ja generell eh nicht so viele geben für die minimale Anzahl an Mattwege)
* Neuer Modus: Freeplay:

  * nicht mehr, um ein spezifisches Opening zu lernen, sondern um selber entweder gegen sich selber oder gegen die Engine zu spielen (einstellbar).
  * Die besten Züge und die Evaluationsbar laut Engine, sollte man optional anzeigen können.
  * Es sollte auch anhand der gespielten Züge ermittelt werden, welches der gespeicherten Openings man gespielt hat und ab wann man den Spielbaum verlassen hat."
* 16.09. 17:24: "Ändere folgendes:
* Allgemein:

  * Die Sounds sind immer noch nicht gut. Ich habe dir ein paar Sounds gegeben, für Figurensetzten (leicht -> Pawns, schwer -> Leichtfiguren oder höher), Schach, Schachmatt, Bestätigung/Ablehung -> für Feedbacksounds (wenn man bspw. beim Rätsel, den nächsten Move erraten hat) -> Es soll keine Sounds geben, für das Benutzen von UI Elementen.
  * Füge einen Slider ein, um die Volumlautstärke andern zu können.
  * Es erscheint Klicken auf die Enginevarianten ein neuer Tab mit der Vorschau, aber irgendwie nur im Freeplay-Modus. Mach das stattdessen wie überall sonst (bspw. wie im Lernmodus), dass beim Draufklicken sich das eigentliche Board ändert (und nicht ein neuer Tab entsteht) und füge überall das Feature ein, dass man durch das drüberhovern über die Enginevarianten eine kleine Vorschau bekommt (über Mauszeiger, verschwindet wenn nicht mehr drübergehovert).
  * Füge eine Sprachauswahl mit neben "Deutsch" einer neuen Option "Englisch".  Füge im Projekt eine Tabelle ein, welche man erweitern kann für weitere Sprachen.
  * Füge die Möglichkeit ein für Custom Skins für das Board (Faben des Brettes und Figurenaussehen) und füge neben dem bestehendem eine Alternative ein.
  * Speichere nach dem selben Prinzip der letzteren beiden Punkte auch die Sounds ein. Lass dabei als Alternative deine eigenen alten Sounds.
* Freeplaymodus:

  * Füge einfach nur einen Hacken, ob die Engine den Gegner spielen soll oder nicht und füge Feature ein Board umdrehen zu können, statt wie bisher zwischen 3 Optionen auswählen zu können.
* Branchmodus:

  * Die Anzeige der Knoten ist noch nicht ganz richtig: Es gibt ja die Färbung Grau (wahrscheinlich hell grau) für Lernstatus "unbekannt" und Dunkel (wahrscheinlich dunkelgrau) für "Gegner/Ende" und bisher färben sich nur die hellgrauen Knoten, wenn man etwas gelernt hat, oder bspw. diese Lernkarten aufschiebt. -> Soweit ich das sehe sind diese Farben (hellgrau und dunkelgrau) entweder unintuitiv oder vertauscht, denn immer wenn ich auf einen hellgrauen Knoten klicke, dann ist zwischen diesem und den vorherigem Zug (jedenfalls bei einem Branchsystem für ein Opening, was aus der Sicht von Schwarz ist) ein gegnerischer Zug passiert (also ein Zug von weiß), aber eigentlich sollten doch diese hellgrau sein (und sich sonst noch farblich ändern können), bei welchen zwischen dem vorherigem Knoten und dem angeschauten ein eigener notwendig ist.
  * Der Gegnerknoten (also der beim Unbekanntstatus) sollte sich auch bei einem Lernfortschritt umfärben können (in einer leicht dunkleren Farbe als der Benutzerknoten). Wenn ein Knoten seinen Lernfortschritt verändert hat, dann sollten alle unmittelbar folgende Gegnerknoten mit eingefärbt werden.
  * Für die Prioritätsauswahl im Branch, aktualisiert sich nicht die Anzeige für die jetzige Priorität des Knoten (und des fortlaufenden Branches), beim Wechsel des Knoten. -> Lösche auch die Möglichkeit "Branch: Priorität wählen", schallt einfach alle standardmäßig auf aktiv, und man braucht aktiv ein Opt-Out.
  * Die Auswahl der Stellungsvariante bringt nichts (jedenfalls visuell, weil sich das Board nicht entsprechend ändert, vielleicht hast du das ja tatsächlich eingefügt), weil scheinbar bisher nur das Feature zur Erkennung bzw. zum Forcen von Transpositions eingefügt wurde, aber nicht für Variationen (bei denen aus unterschiedlichen Gegnerzügen trotzdem dieselben Benutzerzüge resultieren) (sollte ja den Lernprozess vereinfachen).
  * Am Ende jedes Branches, hast du ja, wie gefordert, eingefügt, dass die möglichen sinnvollen Gegnerzüge miteingespeichert werden. Diese sollten aber im Branch kompakter dargestellt werden (in einem Knoten) -> ähnlich wie du das anzeigst mit den Transpositions und der Kompression zu selben Position, (indem du in den Knoten, die Züge in Slashes auftrennst,) kannst du dasselbe ja auch für die Branchenden machen mit zusätzlich einer Integration des Ansehens der verschiedenen Stellungsvarianten einzeln -> benenne dabei die Stellungsvarianten nicht nach 1/2, 2/2, sondern nach dem benötigtem Gegnerzug (Be2, Kc4, ...).
* Custom Opening Generator:

  * Gestallte generell die Variabelsetzung ein bisschen übersichtlicher, obwohl du noch zusätzlich hinzufügen sollst, dass man eine manuelle Anzahl von Tiefenabschnitte festlegen kann (bspw. zwischen 1-5). Bisher ist ja jede Zeile für eine Variable ausgelegt (wie bspw. für "Komplex: ab so vielen plausiblen Antworten"), nun soll sich die Anzahl der Variablen pro Zeile je nach Anzahl der Tiefenabschnitte erhöhen d.h. bspw. würde man dann visuell in der UI sehen, bei Abschnittsanzahl = 3, je 3 Inputmöglichkeiten für folgende Parameter horizontal nebeneinander: "Abschnittstiefengrenze", "Komplexitätsgrenze", "Leichtigkeitsgrenze", "Enginesuchtiefe", "maximaler gegnerischer Verlust", "maximaler Kompressionsverlust". (Benenne die jetzigen Variablen in diese neue Namen (funktional sollte sich nichts ändern und die Zuordnung sollte eindeutig sein) um und füge beim Hovern eine Beschreibung ein, was diese bedeuten.) -> Zur Klarstellung jede dieser \~6 verschiedene Variablen haben immernoch ihre eigene Zeile, aber gerade gibt es noch einen sehr großzügigen Platz bei den jeweiligen Umstellen der Werte dieser Variablen. Genau letzterer Platz, soll man anhand der Anzahl der Tiefenabschnitte verändern können, sodass jede Variable im Code eine Liste/Array von Werten ausgeben kann (jeweils pro Abschnitt einer).
  * Der Human-Moves-Filter, sollte zwar im Custom Opening Generator erstellbar sein können, aber das sollte nur für ein Label für jeden betroffenen Knoten sorgen. Das eigentliche Ausblenden sollte im Branchmodus optional sein.
  * Die Variablen sollten durch das Scrollen mit dem Mausrad, während man zufällig über die Werte hovert, nicht veränderbar sein, aber die Scrollfunktion, um die anderen Variablen zu sehen sollte natürlich bestehen bleiben (, also die Sliderfunktion sollte mit dem Mausrad weiter möglich sein, ohne dass man umbedingt seinen Mauszeiger über dem Slider haben muss).
  * Die Lernkartenvorhersage ist jetzt viel zu tief.
  * Die Auslastung pro Thread ist weiterhin zeitlich recht variabel. -> im Taskmanager sehe ich, obwohl ich 8 Arbeiter benutzen können wollte (a 2 Threads), dass nur zwischen 1 bis 5 stockfish-windows-x86-64-universal.exe ausgeführt wurden (und das liegt nicht an meiner CPU, weil sie maximal 50% Auslastung hatte, im Durchschnitt noch weniger) -> kann man die Engine nicht irgendwie konstant laufen lassen oder so?
* Lernmodus:

  * Wenn man Leertaste drückt, und die Lösung noch nicht angezeigt wurde, dann soll (nur) die Lösung angezeigt werden."
* The 5h Limit ended before success. 16.09. 22:59: "Arbeite daran weiter, was du nicht beenden konntest."
* 17.09. 2:47: "Ändere folgendes:
* Allgemein:

  * Gerade muss man noch ein bisschen Warten (\~2-3s) bis man die erste Bewertung für seine aktuelle Stellung bekommt (v.a. bei der Evaluationsbar). Benutze mehrere Threads, um mehrere Engines gleichzeitig zu runnen, welche die Position bewerten, geordnet nach ihrer Suchtiefe. Eine superleichte (Lösung schon nach paar Milisekunden), leichte (Lösung nach \~0,5-1s) und schwere (Lösungs nach max 10s -> ähnlich wie bisher). Die Evaluationsbar soll sich entsprechend dem neusten Stand ändern.
  * Wenn der König im Schachmatt ist, sollte er sich um paar Grad leich neigen und rechts oben über ihm sollte entweder ein X für Matt oder ein - für Remi auftauchen (nur beim betroffenem König).
  * Wenn ich das erste mal über die Enginevarianten hovere, erscheint das Spielbrett zuerst oben links vom gesamten Bildschirm und nicht über dem gehovertem, wie sonst auch.
  * Da wo man drüberhovert, sollte nicht wie jetzt die linke untere Ecke des Spielbrettes sein, sondern die untere Kante mittig sein.
  * Man sollte nicht mehrere Optionentabs gleichzeitig geöffnet haben können. (Beim wiederholten drücken des Optionenknopfes, einfach den Originaltab wiedererscheinen lassen.)
  * Die Figuren beim alternativem Stil sehen leicht komisch aus beim unteren Stück, welche ihre Haltung zum Boden darstellen soll. Dieses Haltungsstück besteht aus zwei Formen, welche 3d-Zylinder darstellen sollen und noch drumherum eine Ellipse. Diese Ellipse ist eigentlich nicht notwendig (, vielleicht guckst du dir diese Sprites, des alternativem Stil selber nochmal genauer an).
* Custom Opening Generator:

  * Wenn der Benutzer noch nicht das Namentab angefasst hat, dann soll sich der Name entsprechend der Anzahngszüge live mitverändern bspw. "e4 d5 ...".
  * Warum sollte man Threads je Arbeiter einstellen können? Ich dachte der benötigt fest immer 2? (Wenn dem nicht so ist, dann schreibe auf in deiner Beschreibung beim Hovern, was das genau verändert, wenn doch, dann lösche diese Möglichkeit die Variable zu ändern.)
* Branchmodus:

  * Bei mehreren Varianten einer Stellung, füge ein, dass automatisch zwischen den Stellung durchgewechselt wird visuell im kleinen Spielbrett. Bei der Auswahl einer spezifischen Stellung, soll sich das nicht mehr verändern.
  * Wenn es innerhalb eines Knoten zu viele Variationen gibt, dann vergrößere entsprechend den Knoten.
  * Füge bei der Auswahl eines Knoten im Branchmodus den Knopf "Jetzt diesen Branch lernen" ein. Damit wird man zum Lernmodus geschickt und man lernt zuerst die kürzeste Knotenabfolge bis zum ausgewähltem Knoten, dann arbeitet man sich weiter anhand des Branchsystems aus durch.
  * Teilweise gibt es im Branchsystem eine noch zu große Anzahl an vermeidbaren Überkreuzungen von Wegen zu Knoten. -> Bitte weiter das optimieren.
* Freeplaymodus:

  * Man kann nicht, über diese Anzeige der gespielten Züge, zurück zu alten Stellungen gehen. Behalte das bei wie bei den anderen Modi.
* Rätselmodus:

  * Füge einen neuen Untertab ein für den Rätselmodus, sodass man die Rätsel die man macht Customizen kann. Füge folgende Parameter ein: Zeit pro Spielzug (optional), Zeit insgesamt (optional), Rätsel-Schwierigkeit, Leben (optional), Bei Fehler Lösungszug anzeigen (bool), Typen von Aufgaben (Liste von bools -> Matt in N, Materialgewinn, Remis erzwingen (wäre neu), ...) und was dir sonst noch einfällt.
  * Füge die Möglichkeit ein, dass man für seine Kombination an Parameter (welche ich dir gerade aufgezählt habe), Presets erstellen kann. Jeder dieser Presets hat dann eine High-Score Bewertung + Anzeige an insgesamt durchgeführte Rätsel (für Presets in welchen man nicht verlieren kann, weil es bspw. keine Leben gibt, gibt es keinen High-Score).
  * Füge den Knopf ein im Hauptmenü "Schnelles Spiel mit alten Einstellungen".
  * Füge Basic-Schachendspiel-Rätsel ein für Schachmatt mit Dame, Turm, 2 Läufer, Läufer+Pferd, Pawnverteidigung (dass man eine Engine theoretisch mit unendlich vielen Zügen davon abhalten können muss den Pawn zu Promoten -> natürlich nur bei Stellungen, in welchen das möglich ist, und es soll einen Thresshold von Zügen geben bspw. bei 10 Zügen hintereinander ohne Promotion/Bewegung passt), Pawnpromotion, Komplexe Pawnendgames, ... -> all diese Modi müssen automatisch und akkurat generiert werden können (im besten Fall u.a. mit Enginekontrolle) und diese sind unabhängig von den Rätseltypen."
* 17.09. 17:17: "Ändere folgendes:
* Allgemein:

  * Die Drehachse des Königs ist bei Matt/Remi falsch -> diese sollte eher da sein, wo sein Sockel ist
  * Das X und das -ist im Kreis, in welche jene sich befinden nicht mittig.
  * Das nächste Rätsel, sollte auf Zeit automatisch weitergehen (nachdem die Bestätigung ertönt ist), sonst sollte das nächste Rätsel über Leertaste oder Enter automatisch weitergehen.
  * Gerade wird praktisch auf einmal überall in der Software, wenn man bspw. eine Figur spielt, oder sich das Board anzeigen lässt zuerst auf eine Antwort eines der Engines gewartet (sei es auch Animationen zum Bewegen der Spielfiguren (-> teilweise wird auch die Animation zurückgesetzt und erst anschließend wieder angespielt), die Möglichkeit des nächsten Spielen einer Spielfigur, ...). Diese Enginebewertung sollte aber auf einem seperaten Threads laufen und eher im Hintergrund als Add-On interpretiert werden (außer wir reden davon, dass man selber gegen eine Engine spielt, oder dass man eigene Lernkarten generiert).
  * Neben dem Aspekt, dass man die ganze Zeit, auf die Enginebewertung warten muss, was die Software sich ultra langsam anfühlen lässt ist das auch visuell nicht zu gut gemacht. (Bezug spezifisch auf Freeplay, aber eigentlich überall mind. ähnlich) Bspw. habe ich folgende Probleme entdeckt:

    * die Anzeige der Top 3 Bewertung blinkt bei einer neuen Position/Zug die ganze Zeit auf und schließt sich wieder, bis alle Engines ihre Arbeit erledigt haben. Sie ist praktisch, bevor die Engines die gesamte Arbeit erledigt haben, nahezu die ganze Zeit geschlossen. Mach das lieber so: Wenn kein Resultat von keiner Engine, dann pseudo UI mit fehlenden Werten (da taucht ja immer so Zeilen auf mit Slidern, einfach diese ohne Inhalte erstellen lassen). Wenn eine Engine fertig ist, diese Zeilen befüllen und warten bis neuer input kommt, nicht alles wieder direkt löschen. Erst wenn du die Werte der nächsten Engine tatsächlich hast, dann erst neu schreiben.
    * Das Häkchen für die Top 3 Anzeige hat auch ein Delay, egal ob man sie an- oder ausschaltet. Dieses Häkchen soll direkt nach Druck umswitchen und bei an schalten erscheint entsprechend besagte Pseudoanzeige bis erste Engine was ausgibt und bei aus soll direkt diese Top 3 weggehen und v.a. nicht mehr neu für die Evaluationsbar die Bewertung neu berechnen, weil wir haben das ja schon mal berechnet.
    * Wenn nur die Evaluationsbar oder nur Top 3 zu sehen ist, speichere trotzdem für den jeweils anderen das Ergebnis vor, damit das nicht immer neu berechnet werden muss
    * Beim betätigen vom Bewertungs-Häkchen ist es genauso, dass die Engines neu gestartet werden, obwohl das ganze schon gemacht wurde (egal ob vorher an oder aus). -> Unnötig
    * Füge ein, vorausgesetzt du hast den Wert eh nicht schon zwischengespeichert (weil Bewertung und Top3 vorher aus waren), dass es auch für die Evaluationsbar eine Pseudoanzeige gibt wo die Evaluation +-0 ist.
    * Selbiges unnötiges Warten auf die Engine (ohne dass man währenddessen was machen kann) gibt es im Freeplaymodus beim Drehen des Boardes, neue Partie und Zug zurück, beim Branchsystem das anklicken eines Boardes, bei jedem Zug im Rätselmodus, wo das ganze ja schnell gehen muss (bzw. vorgespeichert sein sollte), und beim Custom Opening Generator.
  * Lass für die Enginebewertung die starke Engine für die Gegnerseite nicht mehr arbeiten, wenn man gegen die Engine spielt, weil diese ja eh zu lange braucht, um angezeigt zu werden.
  * Füge Premoves ein. Sodass man auch schon die Spielfigur bewegen kann, bevor der Gegner (die Engine) seinen Move gemacht hat (beachte, dass man auch illegale Moves unterstützen muss wzB. das diagonale Schlagen mit dem Pawn in der Luft/gegen eigene Figur, oder das bewegen einer Spielfigur, durch eine gegnerische hindurch und erst nach dem Premove geschaut werden muss, ob das ein gültiger Move ist -> man weiß ja vorher nicht was gespielt wird).
  * Bei Remi dadurch, dass sich der König nicht weiterbewegen kann, dann nur dieses Remizeichen + Neigung bei dem, der sich nicht bewegen kann. Bei Remi dadurch, dass nur noch Könige auf dem Spielbrett sind, sollten bei Remizeichen + Neigung bekommen.
* Rätselmodus:

  * Trenne das Endspieltraining vom Rätselmodus komplett (zwei eigene Tabs). -> Der erste Tab wie jetzt mit den Presets (mit auswählbaren Taktikaufgaben und Einstellungen), für das Endspieltraining soll nur eine Anzeige aufploppen, wo man sehen kann, welches Endspiel man trainieren möchte (nur eines der möglichen Auswahl, aber jeweils so oft man mag).
  * Das mit dem Presets funktioniert nicht richtig -> bevor ich mein erstes Preset gespeichert habe, gab es schon in der Liste bestimmt \~10 Prests die den Namen "Training" hatten. Diese sind glaube ich vergangene nicht selber gespeicherte Trainings und entsprechend sollten diese auch nicht als Preset angezeigt werden. Füge auch das Löschen von Presets ein.
  * Wie schon angedeutet braucht es zu lange bis die nächsten Züge gespielt werden bzw. angenommen werden (du hattest das ja vorher gespeichert, ich glaube aus irgendeinem Grund)
  * Füge den Knopf ein "Gesamte Lösung anzeigen". Beim Endspieltrainer funktioniert er wie erwartet, dass die optimalste Lösung angezeigt wird, beim Rätselmodus auf Zeit gibt es nur den Knopf "Nächstes Rätsel", wenn noch Leben dabei ist, dann verliert man beim betätigen nur ein Leben, bei Rätsel ohne Leben und ohne Zeit gibt es diesen weiterhin normal.
  * Manchmal ist Bauernumwandlung-Endspieltraining zu einfach, wenn schon auch mal die Lösung sein kann, dass man nur den Pawn pushen muss, dann mach es so knapp wie möglich, denn wenn bspw. mein Pawn noch 2 Züge braucht und der gegnerische König auf der anderen Seite des Boards ist, dann ist das nicht hilfreich zum lernen. Probiere dabei eher Lösungen zu fördern die mindestens auch einen König-Move voraussetzen und die Promotion sollte länger brauchen als ein Zug. Man soll auch aus der Sicht von schwarz spielen können, nicht nur weiß.
  * Manchmal ist Bauernverteidigungs-Endspieltraining zu einfach. Die Remis sind, wenn man am Ende keinen Pawn schlagen muss in Ordnung. Probiere aber bei Remis, wo es darauf ankommt, dass man den letzten Bauern schlägt, ob man nicht auch noch mehrere Bauern einfügen kann, welche die Situation ein bisschen schwieriger gestalten können (dabei sollte man nicht nur Pawns dort plazieren, wo der König eh auf dem Weg ist, sondern es sollten Situationen entstehen, dass ein Pawn auf einer Seite und ein Pawn auf der anderen Seite ist, mit abschätzen, wo man zuerst hingehen sollte -> vorausgesetzt ein Remi ist möglich). Man soll aus der Sicht von weiß spielen können, nicht nur schwarz.
  * Bei Komplexe-Bauernendspiele-Endspieltraining ist eigentlich immer eine akzeptierte Lösung, dass man direkt Promoten kann mit einem Bauern und dann ist das Rätsel zu Ende. Generiere das doch eher über Komposition (jede Seite mindestens ein Bauer, insgesamt mindestens 3 Bauern) und zum Matt muss mindestens von der eigenen Seite 3 verschiedene Spielfiguren bewegt haben. Man soll auch aus der Sicht von schwarz spielen können, nicht nur weiß.
  * Füge auch folgende Endspiele hinzu: Bauern+Läufer und Bauern+Pferd. Neben die Könige gibt es in diesem Modus nur die hier erwähnten Figuren. Bei dem Läufer-Modus gibt es max 1 Läufer pro Seite und bei Pferd-Modus max 2 Pferde pro Seite (weniger sind möglich und sollten auch die Regel sein). Sorge dafür, dass für die Promotion eines Bauern eine Leichtfigur mind zwei Züge machen muss zur Unterstützung. Es muss aber nicht mit Promotion enden, sondern es kann auch ein Matt sein nur mit Bauern+Leichtfigur oder bei schlechter Lage trotzdem zu einem Remis zu kommen.
  * Füge für Endspieltraining ein optionales Häkchen ein, ob man darauf geschaut wird, ob ich in der geringst möglichen Anzahl von Züge das nötige Matt bzw. Remi hinkriege.
  * Entferne diese Fehlermeldung, welche kommt, wenn man auf "Nächstes Rätsel" drückt (vorausgesetzt es gibt keine Leben).
  * Die Taktikrätsel für Remi erzwingen, sollten sich nicht nur auf Bauer+König beziehen sondern auch mit komplexerer Komposition oder mit Engineselbstspiel erreicht werden.
* Branchmodus:

  * Auch hier besteht das auf Engine warten Problem.
  * Das Branchsystem ist unübersichtlicher als in Version 6 geworden, weil du auf einmal nicht mehr die Knoten zentralisierst in Abhängigkeit von den folgenden oder vorherigen Knoten.
  * Erhöhe auch den Abstand zwischen Knotenreihen (Knoten sind ja in Spalten aufgeteilt je nach Tiefe) um das 1,5-fache vom jetzigen.
* Custom Opening Generator:

  * Auch hier besteht das auf Engine warten Problem.
  * Mach die Veränderung des Kartenlimits in 10ner Schritten bei betätigen eines Pfeils.
  * Das mit den Threads pro Arbeiter, hast du immer noch nicht entfernt (vorausgesetzt meine These stimmt, dass jeder Arbeiter 2 Threads braucht) bzw. keine Beschreibung, wie bei den anderen Variablen verfasst, was das konkret ändert."
* 17.09. 22:19: "Arbeite daran weiter, was du nicht beenden konntest."
* 21.09. 1:11: "- Allgemein:

  * Wenn ich die App öffne, kommt ein mehr oder weniger random Sound.
  * Bei wechselndem Status, sollte trotzdem die Evaluationsbar nicht so stark klippen. Lass das ganze ein bisschen Smoother sich bewegen (nach 0,1s sollte aber die Zielposition erreicht sein).
  * Beim Wechsel auf Vollbild, wird das Brett manchmal nicht neu gemalt und deswegen muss ich erst nochmal auf das Brett drücken, damit es sich aktualisiert.
  * Im Menü befindet sich bei jedem Opening über dem "Lernen"-Knopf eine graue Box von links, welche den Knopf leicht verdeckt.
  * Bei den anklickbaren Top 3 Enginevarianten, sollten die Züge nicht mehr in blau (so Linkartig) dargestellt werden -> Die Anklickmöglichkeit sollte aber bestehen.
  * Ändere die Liste der gespielten Züge so, dass es zwei Spalten gibt. Die eine Seite für Weiß und die andere für Schwarz.
* Rätselmodus:

  * Zeige auf der oberen rechten Seite des Bildschirms größer an, ob man für Matt in 3 spielen soll o.ä.
  * Wenn ich vorher eine Figur ausgewählt habe (, mir die möglichen Wege über die grünen Punkte, welche die Figur zurücklegen kann, angezeigt werden,) und auf ein neues Board gehe (bspw. indem ich auf "Nächstes Rätsel" drücke), bleiben die grünen Punkte erhalten.
  * Im Remis-sichern-Taktikmodus gibt es praktisch nur Züge, welche nach einem Move gelöst sind. Erhöhe die Komplexität, indem mehrere aufeinanderefolgende Züge nötig sind, damit das geschieht. (Wenn das in Schach möglich ist.)
  * Beim Endspieltraining wird bei manchen Modi das Häkchen schwarz "Kürzesten Bewiesenen Lösungsweg verlangen". Versuch das für alle möglich zu machen. Es muss nicht komplett bewiesen sein, aber nimm dort alle Wege auf, welche die geringstmögliche Menge an Zügen benötigt, um die geforderte Lösung zu erziehlen.
  * Bei Bauernverteidigung bei Endspielübung geht es praktisch nur noch darum zum Pawn hinzulaufen, und ihn zu schlagen. Aber in Schach gibt es ja noch die Möglichkeit, das man auf das unendliche vor dem Bauern stehen muss und verteidigen muss, ohne dass man selber den Pawn schlagen kann. Das fehlt mir in diesem Modus.
  * Der Modus "Komplexe Bauernendspiele", versteht nicht, wann ich falsch liege. Er wird mir die ganze Zait nur anzeigen, dass ich alles richtig mache.
  * Im Rätselmodus, sollte klarer gemacht werden, welche Farbe man selber ist.
  * Entferne doch wieder die Bauern+Leichtfigur Modi.
  * Der Modus, Bauernverteidigung mit mehreren Bauern hört zu früh auf (bevor ich alle Bauern ausgeschaltet habe). Mache diesen Modus mit eigenen Bauern und eine Leichtfigur (welche man unbedingt bewegen muss), gerne manchmal noch komplexer -> sollte immer in Remi Enden, während der Gegner eigentlich eine aus der Sicht eines Menschen eine bessere Stellung hat, weil er gleich promoten würde.
* Branchmodus:

  * Füge innerhalb des Branchmodus ein: Human Moves neu generieren.
  * Innerhalb eines Knoten lässt du oft zu viel horizontalen und vertikalen Platz für die möglichen Varianten ein. Sei weniger großzügig.
  * Wenn es non human moves gibt, innerhalb von Varianten, dann färbe noch zusätzlich den betroffenen Zug innerhalb des Knotens im selben pinkt/lila Ton.
  * Beim "Jetzt diesen Branch lernen"-Knopf sollte man nach dem lernen (wie sonst beim normalen lernen auch), rückwärts vom Branch weiter gehen und ab da dann die Branches lernen. (Also sagen wir, wir haben folgenden gelernten Branch A->B->C->D->E->F, dann soll er nach Abschluss dieses Branches zuerste gucken, ob es noch unterbranches von E gibt, und diese lernen, das selbe für D, dann C, ...). Gerade ist es nämlich so, dass nach dem erlernen, man im Lernstatidum zurück zum Anfang geschickt wird.
* Lernmodus:

  * Erstelle (wie beim Generator, Branch oder Rätsel auch) einen Extra Tab für den Lernmodus.
* Neu:

  * Allgemein: Füge für jede Seite eine Anzeige an, welche Figuren von der Gegenseite schon genommen wurden (einfach visuell die Bilder der Figuren benutzen) und daneben eine Anzeige mit +/- int, welche anzeigt was die Summe der Punkte ist zwischen dem vom Benutzer genommenen Figuren und vom Gegner.
  * Lernmodus: Zeige die Lernfortschrittsanzeige auch beim Lernen an.
  * Neuer Bereich: Eigene Spiele:

    * Möglichkeit einbauen entweder manuell oder die von Chess.com heruntergeladenen Games upzuloaden (Beispiele habe ich dir im Dateisystem gegeben.) -> achte darauf, dass man entweder dir nur ein Game schicken kann oder mehrere, aber alle in eine Datei.
    * Möglichkeit einbauen das normal sehen zu können mit Evaluation, selber bei einem alternativszenario weiterzuspielen, ...
    * Einer Möglichkeit das Game zu Evaluieren, indem man sich die Highlights des Games anguckt (-> mit Sortierung der Züge nach: Lektion, bester Zug, Patzer, Gut, Schlecht, Verpasste Chance)
  * Rätselmodus:

    * Füge einen neuen Tab ein, mit welchen man aus seinen vorherigen Fehlern lernen kann durch den Upload seiner eigenen Spiele -> durch die Analyse, werden die eigenen verpassten Chancen oder Patzer gesammelt und dann kann man die nochmal spielen. (Mach eine optionale Anzeige, dass die letzten x (z.B. 10) Spiele ausgeschlossen werden sollten, weil die zu nah dran sind.)
    * Füge einen neuen Taktik Modus ein, mit welchem man das Mid-Game besser erlernen kann. Dabei werden relevante Positionen genommen beim Engine Selbstspiel und der Spieler muss auswählen welcher Zug vermutlich gut ist. Eine relevante Position ist dadurch definiert, dass eine handvoll Züge (einstellbar, normal zwischen 2-4) den Vorteil beibehalten, während der Rest es nicht tuen. Dabei werden jene Züge ausgeschlossen welche durch einen Captur unmittelbar danach den Vorteil resultieren. -> Es soll v.a. um Vibe Züge gehen.
  * Branchmodus:

    * Füge die Möglichkeit ein Subbranches zu erstellen und wieder zu entfernen (nur den Status Subbranch, nicht dabei Knoten löschen). (Kann man dann im Branch ein aus ausblenden für Übersicht.)
    * Füge die Möglichkeit ein Branchsysteme miteinander zu kombinieren (sollte man ja gut machen können, weil bei jeden ist ja die Neutralstellung die Erste -> einfach da was gemeinsam kombinieren, Rest einbinden) -> neues Branchsystem wird dabei erstellt, die alten beiden werden beibehalten.
    * Füge die Möglichkeit ein, dass man manuell dann über den Custom Opening Generator direkt (vom Branchmodus Tab aktiviert) eine Stellung weiter erforschen kann (bspw. wenn man häufig dagegen gespielt hat). Dabei wird automatisch das erstellte Branchsystem als Subbranch integriert -> neuer Knopf, wenn Knoten ausgewählt z.B. "Tiefer Erforschen"
  * Custom Opening Generator: neue Parameter: Welche bei Knotenausschluss durch zu hoher Komplexität, trotzdem noch die x-Besten Züge weiterverfolgt werden (standard ist 0 -> bedeutet, Funktion aus), jeweils nur, wenn sie eine Evaluation von y haben (Standard ist 0 -> normale Evaluationsbar anzeige, wenn ausgegelichen)"
* 21.09. 8:45: "Arbeite daran weiter, was du nicht beenden konntest"
* 23.09. 1:29: "Ändere folgendes:
* Allgemein:

  * Genommene-Figuren-Anzeige:

    * Stilistisch könnte es ein wenig schöner sein: Gerade ist es so, dass die genommenen Figuren noch einen sehr großen Abstand zueinander haben in der Anzeige. Mach es stattdessen so für Pferd, Läufer und Turm, dass gleiche Figuren, sich übereinander liegen mit einer leichten Versetzung nach rechts (Hälfte der Breite der Sprites), während der Sprite der rechten Figur über die der linken liegt und um \~20% größer ist. Bei Pawns bleibt die Versetzung für die ersten 3 Figuren diesselbe, wobei nur die mittlere Figur größer ist und davorsteht. Bei mehr als 3 genommenen Pawns steht links eine Zahl, welche die insgesamt genommenen Pawns dieser Seite beschreibt (also 4, 5, 6, 7 oder 8). Unterschiedliche Figuren untereinander sollten auch ihren Abstand leicht verringern, auch um \~20%.
    * Anzeige ist teilweise auch falsch: Beim Boardspawn und bei Figurenangriff sollte sich die Anzeige so updaten, dass das angezeigte Board pro Seite mit einem vollen Set verglichen wird. Entsprechend wird die Anzeige nicht mehr fehlerhaft beim Spawn eines Boardes durch ein Rätsel oder beim Lernmodus (wenn Figuren schon genommen wurden) und beim Schlagen eines promovierten Bauern (wenn sich zwei Damen der gleichen Farbe auf dem Feld befinden und eine davon geschlagen wird, dann erscheint auf der Anzeige, dass die Dame geschlagen wurde (obwohl sich nur die Pawnanzahl erhöhen sollte in diesem Szenario)). Die +/- Anzeige für Schwarz oder Weiß sollte dabei die Punkte, der auf dem Feld befindlichen Figuren sammeln und daraus die Differenz bilden -> also zwei unterschiedliche Mechanismen für die Anzeige, um mit der Promovierung zurrecht zu kommen.
    * Die Ordnung der Anzeige ist auch nicht so wie ich es mir vorgestellt habe: Streiche das B bzw. W neben der +/-Anzeige, stelle die Figuren unten rechts und oben links auf. Vom Boardrand zur Mitte sollte die Reihenfolge so sein: Queen -> Turm -> Läufer -> Pferd -> Pawns -> +/- Anzeige. Halte dabei keinen Platz frei für Figuren, welche noch nicht genommen wurden, sondern lass die Anzeige sich einfach lateral zur Boardmitte hin vergrößeren beim Schlagen einer einzigartigen Figur bei entsprechender Farbe -> die +/- Anzeige und die untergeordneten Figuren würden sich entsprechend zur Mitte hin verschieben. (Beachte auch, dass eine Vergrößerung erfolgt, wenn eine Zahl neben den Pawns auftaucht, bei mehr als 3 genommenen Pawns.)
  * Spielverlaufanzeige: Neu: Lass bei den betroffenen Zügen vom rechten Rand aus der Kästchen/Buttons eine zusätzliche Anzeige zeichnen für relevante Geschehnisse. Relevant sind dabei das nehmen einer Figur (zeichne die Figur mit einem Strich dadurch), beim Promovieren (Bauer; Pfeil zu; neue Figur), Schachsetzen (Königfigur), Schachmattsetzen (Strich durch betroffenen König)
  * Der Bug besteht weiterhin, dass wenn man eine Figur ausgewählt hat und sich das Board wechselt, dass die grünen Punkte bestehen bleiben (bzw. ändern die sich in Schlagoptionen -> alles nur rein visuell, wenn man wegklickt, funktioniert es wieder)
* Eigene Spiele - PGN:

  * Benenne den Button um in Eigene Spiele \& Analyse
  * Überarbeite die Funktionsweise von PGN einfügen:

    * Benenne ihn um in "PGN schreiben"
    * Habe dort schon ein vorgefertigtes PGN vorbereitet im folgendem Stil (stell dir vor /n bedeutet Enter): "\[Event ""]/n\[Site ""]/n\[Date ""] ..."
    * Neu: Mache es möglich anhand der schon eingefügten PGNs eine Autofill-Funktion pro Zeile zu machen, welche man mit Tab bestätigen oder mit Pfeiltasten die Empfehlungen durchswitchen kann.
    * Neu: Für die Züge, sollte automatisch 1./2./3./... geschrieben werden (bzw. nicht geschrieben, wenn man das selber so eingibt bzw. so herauskopiert). Es sollte ein Board auftauchen, wo man die aktuelle Stellung sieht und die Züge auch vom Board aus erweitern kann. Es sollten nur legale Züger erlaubt werden.
  * Entferne Manuell Eingeben Button
  * Füge die Möglichkeit ein Matches zu entfernen
  * Überarbeite die Partie-Analyse:

    * Neu: Erstelle im Einstellungsmenü einen neuen Tab für die Partie-Analyse mit welcher man folgendes einstellen kann:

      * Parallele Arbeiter für die Analyse: int = 8
      * Threads je Arbeiter: int = 2
      * Enginetiefe
      * Patzer-Threshold: float = -2,0 (Ab dieser Verschlechterung der Enginebewertung, im Vergleich zum Besten Zug, wird ein Zug als Patzer bewertet)
      * Schlecht-Threshold: float = -0.75
      * Verpasste Chance-Unsicherheit: float = 0.5
      * Maximale Möglichkeiten Verpasste Chance: int = 3 -> anders benennen; so viele Möglichkeiten darf es maximal geben nach einem Patzer des Gegners, die bessere Evaluation mehr oder weniger zu behalten, um bei Missgelingen als Verpasste Chance gewertet zu werden, sonst Patzer-Wertung
      * Min. ein Wert, welcher beschreibt in wie weit sich der Threshold an den Rändern ändert -> eine Engineevaluationsänderung von -11 zu -18 (eher kein Patzer), sollte nicht so stark gewichtet werden, wie eine von 0 zu -2.5 (Patzer).
    * Zur Info: Verpasste Chance soll nur bedeuten, dass wenn der Gegner im vorherigem Zug einen Patzer geleistet hat und im nächsten Zug die Enginebewertung auf dem alten Niveau wieder ist (mit gewisser Unsicherheit), bei schlechter als Unsicherheit, dann wird es als Patzer gewertet; Gut wenn zwischen Schlecht und bester Zug.
    * Der Wert "Verlust", welcher neben den Patzer o.ä. steht deckt sich teilweise nicht mit der Enginebewertungsänderung, welche ich beim Schachbrett sehe. -> v.a. bei sowas wie Patzer schwerwiegend wo diese Änderung um über 4 Punkte abweicht, aber auch bei Bester Zug, wo der Verlust 0,02 ist
    * Die Anzeige der Bewertung der Züge sollte nicht in einer Liste angezeigt werden, sondern es sollte oben rechts für den jeweiligen ausgewählten Zug die Analyse einzeln angezeigt werden.
    * Neu: Unter der Bewertung von dem Zug, sollte es die Möglichkeit geben den besten Zug angezeigt zu bekommen, oder selber zu verbessern (abhängig von der Bewertung des Zuges).
    * Neu: Füge den Knopf hinzu "Highlights durchgehen" -> mit welchem die relevanten Züge durchgegangen werden -> dabei sollte (auch anhand von den letzten Partien, wer am meisten gespielt hat) klar sein, aus welcher Sicht die Analyse durchgegangen wird
  * Entferne die Filterung der Zugbewertung
  * Lasse den Analyse abbrechen Knopf nur erscheinen, wenn gerade gearbeitet wird.
* Rätsel:

  * Der Tab "Aus eigenen Fehlern lernen" braucht folgende Veränderungen:

    * Bei Chance Nutzen, sollte nicht nur der beste Move akzeptiert werden, sondern alle Moves, welche die Evaluationsverbesserung mehr oder weniger noch behalten
    * Dir Rätsel sollten durchmischt sein und nicht chronologisch (vorausgesetzt, das ist nur gerade so, weil ich, weil ich beim Testen nur eine Partie analysiert habe, sonst (bzw. eigentlich so oder so) füge einen optionalen Button, welcher auch für eine automatische Analyse von fehlenden Matches auch noch sorgt, sodass wir mehr verschiedene Rätsel haben)
    * Lass auch gesondert anzeigen was ich im Match damals gespielt habe.
  * bei Bauerverteidigung Endspieltraining auch einfügen, dass man auch ein Remi erzwingen kann, wenn Gegnerkönig nur hinter seinen Bauern gelangen kann (bisher sind die Lösungen entweder den Bauern direkt zu nehmen, indem man schneller ist oder wenn sich der Bauer in der Ecke befindet)
  * bei komplexe Bauernendspiele:

    * es wird teilweise ein Zug als falsch gewertet, obwohl es bei "Lösung anzeigen" tatsächlich der richtige war
    * Neu: Versuche alle akzeptierten Möglichkeiten für den nächsten Zug vorzugenerieren, um das Laden zu reduzieren
    * Manchmal passiert bei "Lösung anzeigen" auch gar nichts -> scheinbar, weil nicht mehr die richtigen Folgezüge akzeptiert werden
  * Kombiniere die Modi Bauernverteidigung mit Bauernumwandlung und komplexe Bauernendspiele mit Bauernverteidigung mehrere Bauern miteinander
  * Spalte den Mittelspielmodus in zwei:

    * einen wo die guten Züge eine deutlich bessere Bewertung erzielen, als alle anderen Züge
    * einen wo die guten Züge nur eine leicht bessere Bewertung erzielen als die anderen -> Enginetiefe höher
  * Im Mittelspielmodus habe ich folgende Fehlermeldung bekommen: "Noch keine passende ruhige Mittelspielstellung gefunden. Erneut vorbereiten oder Anzahl guter Züge erweitern", obwohl die Einstellung bei min =2, max = 5 waren. -> Kontrolliere
  * Beim Remis sichern Taktik Trainer gibt es nur noch dieselben 3 Springergabeln Kompositionen -> bitte mehr verschiedene Remis in 1,2 und wenn möglich mehr einbeziehen
  * Matt in N nimmt manchmal nicht den optimalsten Engine Move -> obwohl Matt in 3 war, hat die Engine die Figur so bewegt, dass ich sie in 2 Matten konnte
  * Neu: Füge einen neuen Tab ein "Gegnerfehler ausnutzen":

    * Man wählt das sich anzuschauende Opening aus
    * Aus dem bestehenden Branchsystem werden sich nun diese Moves angeschaut, welche sich nicht im Baum befinden, weil sie laut Engine ungünstig sind -> der Benutzer muss den nächsten Folgemove predicten -> wenn die Fortsetzung klar ist, weil die nächsten Bewertungen auf eine Movefolge hindeuten (also der beste Zug klar von den anderen Zügen entfernt ist) oder es ein Matt ist, dann soll weitergespielt werden, bis dem nicht mehr so ist
    * Lass diese Stellungen random erscheinen
* Freeplay:

  * Neu: Füge vor dem eigentlichen Spielen einen Plazierungsmodus ein mit der Möglichkeit Figuren zu löschen, umzuplatzieren und zu spawnen.
* Custom Opening Generator:

  * Benenne folgende Parameter um in kohärenter mit den anderen: "Bei Komplexität: Top X behalten" und "Nur ab Bewertung Y", meine Vorschläge wären (am besten fällt dir was präziseres ein) "Komplexitätsgrenzenresitenz (KGR)", "KGR Bewertungsgrenze"
* Branchmodus:

  * Neu: Füge die Möglichkeit ein Subbranches einzeln auf und zu zuklappen
  * Neu: Füge auch eine Möglichkeit des auf und zuklappens (bzw. verkleinern) von Stellungsvariationen ein -> Subbranches sollte eh nur an Knoten erstellt werden können, welche einen eigenen Zug beschreiben
  * Der "Mit anderen Opening kombinieren" Button, sollte nicht die das Dateisystem öffnen sondern eine Auswahl aus dem schon in der App eingefügten nehmen (, die also im Hauptmenü angezeigt werden)
  * Innerhalb eines Knoten bei mehreren Stellungsvariationen sind die Züge ja in zwei Spalten aufgelistet
  * Beim Subbranch-Status umschalten resetet sich auch die Kameraposition, das sollte nur passieren, wenn auf dem gesamten Bildschirm kein Knoten mehr zu sehen ist, sonst bleibt die Kamera an der Originalposition
* Neuer Modus: Schach spielen auf einem Gerät (umbenennen):

  * direktes eintragen in eigene Spiele über PGN -> Eintrag notwendig, wer man selber war
  * Schachuhr (auch asymmetrisch), Zeit hinzufügen pro gespielten Zug, ...
  * automatisches Ansichtswechsel bei Zug (optional)
  * keine Engines"
* 23.09. 8:44: "Arbeite daran weiter, was du nicht beenden konntest"
* 23.09. 14:33: "Da V10 deutlich größere Ausmaße angenommen hat, als wahrscheinlich die Originalstrukturierung von V1 vorgesehen, verändere die Struktur des Projektes, sodass sie deutlich MODULARER, für einen Außenseiter EINFACHER verständlich und sich deine benötigte Tokenzahl voraussichtlich für das Beheben von zukünftigen Bugs oder hinzufügen von Features reduziert. Verändere aber dabei mehr oder weniger gar nichts an der funktionsweise des Codes, aber Zusammenlegung von ähnlichem Code.
Beachte dabei, dass das Projekt zukünftig Git-Hub reif sein muss (-> du sollst mir ausdrücklich noch nicht ein fertiges Git-Hub-Projekt basteln, weil noch Features fehlen und Bugs zu beheben sind, aber nicht Sachen einbauen, welche dann später eh wieder umstrukturiert werden müssen).
-> wahrscheinlich solltest du z.B. besser Unterscheiden, was so in der eigentlichen Git-Hub-Veröffentlichung drankommt und was ich nur zum Testen noch hier habe
Finde selber heraus, wie man die Modularität deutlich verbessern kann, ich kann nur generell sagen, dass es z.B. wahrscheinlich schön wäre für jeden Tab der Rätsel einzeln Code aufgelistet zu haben in einem Subordner (mit einem weiterem Code File wird die Tabanzahl erweitert) oder beim Branchmodus hat jedes Feature (wzB Human Moves neu generieren) seinen eigenen Code oder für das Anzeigen eines Boardes gibt es eine spezielle Funktion auf die mehrere unterschiedliche Codes Files zugreifen und ähnliches.
-> eine Modifizierung des Frontends (UI) steht noch an, also bereite dich bei der Umstrukturierung auch darauf vor (ändere ausdrücklich noch nichts an der UI, aber gehe davon aus, dass diese sich verändern könnte)
Eine bessere Strukturierung deines Arbeitsordners, würde dir wahrscheinlich auch nicht schaden (wenn möglich).
Erhöhe bei der Umstrukturierung auch die Customizability, sodass man die generelle UI umfärben kann (mach auch ein Beispiel dazu) und weitere Customizability-Optionen welche dir sonst noch einfallen (mind. 1).
Versuche auch, wenn vorhanden, ineffizienten Code oder unnötige im Hintergrund ablaufende Prozesse zu optimieren, ohne dabei für den User relevantes zu entfernen (also z.B. NICHT entfernen Vorgenerierung der Enginebewertungen oder Rätselerstellung, damit User keine bis geringere Ladezeiten hat).
Schreibe auch eine nur in dieser Version auftauchende README.md Datei, welche diese Modularitätsverbesserung und Customizabilityerhöhung klar beschreibt. (Im Allgemeinem sollte sich aber für den Benutzer im ersten Blick kaum bis gar nichts ändern, aber die Struktur ist besser)."
* 25.09. 0:39: "Ändere folgendes:
* Allgemein:

  * Ändere das App-Icon zu einem Turm, wie er auf dem normalen Spielfeld existiert
  * Bei dem Schachbrett, um anzuzeigen wer dran ist, füge eine symbolische Anzeige hinzu oben rechts und unten links außerhalb der Ecken des Schachbretts hinzu (Anzeige nicht über Schachbrett, sondern um diese zwei Ecken herum) -> wenn schwarz dran ist dann ist die Anzeige schwarz, wenn weiß, dann weiß
  * Bei der Figurenanzeige die Pawns noch ein bisschen näher (\~20% Näher von der Gesamtkörperbreite) zusammenrücken; die Zahl, welche die genommene Gesamtpawnanzeige darstellt klippt in die Pferde oben links bzw. ist generell bei beiden zu weit weg; die Queen soll die Größe der größeren Figuren einnehmen (also \~20% größer), die Zahl für den Vorteil, wenn bei 0, dann "+/- 0" anzeigen
  * Bei der Anzeige der Züge, ob eine Figur genommen wurde, Schach gesetzt wurde, ... sollten die weißen Figuren so dick sein, wie die schwarzen Figuren + ihre weiße Umrandung und bei mehreren Piktogrammen in einem Zug (z.B. Promotion + Figurennahme + Schach) sollten sie jeweils durch weiße Striche getrennt werden
  * Bei der Anzeige der Züge, wenn man nach oben scrollt dann wird dieser permanente Header (Weiß <-> Schwarz) mehrmals gemalt und erst beim Neuladen (z.B. durch klicken auf einen der Züge erscheint es wieder richtig) (getestet in Eigene Spiele \& Analyse) -> auch beim drüberhovern den drübergehovertem Zug heller erscheinen lassen
  * Füge zu den Customizationoptionen hinzu, dass Piktogramme u.ä. auch verändert werden können
* Menü:

  * Verschiebe die Einstellung "Ähnliche Gegenvarianten als gemeinsame Lernkarte" im Branchmodus
  * Füge für jeden Modus/Button sein eigenes Menü (wenn Schrift auch bestehen bleiben soll, dann füge das entsprechende Piktogramm links von der Schrift -> Piktogramme bestehen aus einem Schwarz Weiß Bild, ohne Graustufen, wobei Schwarz durchsichtig ist) (Namen sind teilweise auf Englisch, finde die entsprechenden Deutschen übersetzungen):

    * Einstellungen nur mit Einstellungssymbol (keine Schrift) -> gibt es ja schon im Lernmodus
    * Ton an/aus mit entsprechenden Symbolen (keine Schrift)
    * Rätsel - Taktik Endspiel wird umbenannt zu "Rätselmodus" und bekommt ein Piktogramm bei welchem ein Bauer gerade umfällt (im 30° Winkel zur Vertikale und hinter ihm dargestellt durch Streifen, damit klar ist, dass er gerade am fallen ist)
    * Custom Opening Generator wird umbenannt zu "Opening Branch Generator" und bekommt als Piktogramm einen alten Computerbildschirm
    * Freies Spiel - beide Seiten/Engine wird umbenannt zu "Custom Game" mit einem 2x2 Schachbrettmuster
    * Schach zu zweit - an einem Gerät wird umbenannt zu "Local Game" mit einem vs. als Piktogramm
    * Eigene Spiele  Analyse wird umbenannt zu "Eigene Spiele und Analysen" mit einem Buch als Piktogramm
  * Ändere das UI:

    * Oben Links bleibt immernoch die Überschrift "Dein Schachtraining" erhalten
    * Oben rechts immernoch Einstellungen + Ton (nur Piktogramme)
    * Links erscheint unter der Überschrift eine neue Spalte, wo folgende Modi der Reihe nach sortiert sind (von oben nach unten): Opening Branch Generator, Rätselmodus, Custom Game, Local Game, Eigene Spiele und Analysen -> Entsprechende Übersetzungen entweder von Deutsch zu Englisch oder Englisch zu Deutsch (gerade noch wild durchmischt)
    * Wenn es der Platz des Fensters erlaubt wird Piktogramm + Schrift angezeigt, sonst nur Piktogramm
    * die rechte deutlich größere Spalte ist besetzt oben rechts mit dem Openings neu laden Button -> bekommt nur noch dieses Neu Laden Zeichen ohne Schrift; oben links 2 Buttons "Schnelle Rätsel" (-> Rätselmodus mit vordefinierten Einstellungen) und "Schnelles Lernen" -> Lernmodus für das letzte gelernte Opening wird geöffnet, welches noch nicht vollständig gelernt wurde/nicht mehr wiederholt werden muss; unten die bekannten Openings
    * Bei der Anzeige der Openings sollte der Fortschritt zurücksetzen und der Opening löschen Button unten links innerhalb der Kachel stehen (also da wo gerade Branchmodus und Opening löschen sind) (Fortschritt zurücksetzten links, Opening löschen rechts davon), während auf der echten Seite der Kachel in dieser Reihenfolge von oben nach unten diese Buttons stehen sollten Lernen, Freeplay, Branchmodus
* Einstellungen:

  * Stelle (wenn noch nicht getan) die Standardsprache auf Englisch -> lasse die aber zum Testen das so wie ich das habe (also auf Deutsch)
  * eine optionale Debug Einstellung einfügen -> es soll ja bspw. bei den Rätseln nicht dargestellt werden "Engine-Selbstspiel" o.ä.
  * Es soll die Tiefe dieser 3 Engines (so viele waren es meine ich für die normale Bewertung der Stellung) customized werden können (auch die Anzahl der gleichzeitig bewertenen Engines)
* Rätselmodus:

  * Erstelle ein Default-Preset ohne Zeit und Leben und nur mit Matt in N, Materialgewinn und Remis sichern -> dieses kann man nicht löschen
  * Sorge dafür, dass für alle Modi, Rätsel schon vorgeneriert sind, damit man nicht warten muss
  * Beim Gegnerfehler ausnutzen Modus auch einstellbar, ob es um jede mögliche Abweichung gehen soll von der besten Enginevariante, oder nur für Zugvarianten, bei welchen ein klarer Vorteil des Benutzers besteht gegenüber den Gegner
  * WICHTIG: Scheinbar wiederholen sich die Rätsel, weil sie einmal vorberechnet wurden (zumindest bei Matt in N Leicht und auch bei Remis sichern, aber ich gehe davon aus, dass es bei allen Modi so ist) -> sorge dafür, dass konstant eine random Generierung von Rätseln erfolgt (über Enginespiel, Komposition o.ä.), sodass immer neue Rätsel nachkommen und das für jeden Modus, alte gelöste Rätsel können automatisch verwerfen werden. -> das Prinzip ist, dass es pro Modus eine Menge von Rätseln gibt (bspw. 10 -> mach das in Einstellungen einstellbar), welche schon lokal generiert wurden, wenn eines dieser in dem Rätseltopf vorhandenen Rätsel gelöst wurde wird entsprechend ein neuer generiert -> mach auch einstellbar für den Rätselmodus, wie viele Arbeiter und Threads maximal verwendet werden können für diese Generierung
  * Ändere das UI:

    * Statt den einzelnen Tabs kommt nach Klick des Rätsel-Buttons ein einzelner Bildschirm mit folgenden Buttons von oben nach unten: Schnelle Rätsel, Taktik, Endspiel, Aus Fehlern lernen und Gegnerfehler ausnutzen
    * Nach dem betätigen eines der Buttons kommen die Einstellungen zum entsprechendem angeklicktem Submodus, wobei oben links ein Zurückknopf erscheint (nur Piktogramm), um Submodi-Auswahlmenü zurück zu gelangen
    * Bei jedem Submodus-Einstellungen erscheint auch der Knopf "Training starten" -> dort wird der Bildschirm zum eingestellten Training, wobei es immernoch diesen Zurückknopf gibt
    * Bei den Taktikaufgaben Einstellungen erschaffe 3 Spalten links für generelle Einstellungen und mittig für die Aufgabentypen -> oben und unten sind die bisher auch bestehende Preset Einstellungen; die ganz rechte Spalte soll ähnlich wie bei den Openings im Hauptmenü die Presets angezeigt werden samt Namen, Training starten, Bearbeiten, Preset löschen
    * Bei den Endspieltraining Einstellungen sollten alle Modi (Subsubmodi) komplett angezeigt werden, welche man mit Hacken beim Training miteinbeziehen kann -> zu jedem Subsubmodi ein eigenes Piktogramm (bei Damenmatt Queen als Piktogramm, ...)
  * Beim Gegnerfehler ausnutzen Modus wird im Training nicht die ganze Zugfolge gezeigt, wie es zu dieser Position kommt (-> wäre zum Lernen hilfreich) -> auch im Branch anzeigen Button einfügen (zum letzten gemeinsamen Knoten)
  * Benenne beim Training bei der Zugfolge die "Startstellung" in "Rätselstellung" um
  * Entferne die Anzeige "Du spielst Schwarz/Weiß" (wurde ersetzt durch, Anzeige wer gerade spielt)
* Branchmodus:

  * der Stellungsvarianten vergrößern/verkleinern Button sollte mittels eines Pfeils am unteren Rand jedes betroffenem Knotens dargestellt werden
  * bei dem auf und zuklappen der Subbranches ist der Pfeil auf der echten Seite des Knoten -> beides als Verlängerung des Knoten
  * durch Rechtsklick auf einem Knoten sollte ein Menü erscheinen mit folgenden Optionen:

    * Subbranch erstellen/löschen (-> erstellen/löschen anzeigen je nach Status des Knoten)
    * Tiefer erforschen
    * aufklappen/zuklappen (wenn Subbrach, je nach aktuellem Status)
    * Lernstatus zurücksetzen (gilt entsprechend nur für den Branch -> vorausgesetzt der Lernstatus ist nicht unbekannt)
    * Lernintention umschalten (bitte besser benennen) -> und es erscheint ein Untermenü mit den Stadien Aktiv, Später \& Ausgeschlossen
    * Zug anschauen -> Untermenü mit allen Stellungsvarianten
  * Bei der Auswahl eines Knoten sollte dieser Knoten irgendwie markiert werden
  * Beim bearbeiten des Branchsystems sollte sich das Branchsystem visuell nicht direkt löschen und dann wieder verändert auftauchen, sondern direkt verändert werden, damit es nicht so flackert
* Eigene Spiele \& Analyse:

  * PGN schreiben:

    * standardmäßig sollte sich die Auswahl innerhalb der Gänsefüße von \[Event ""] stehen
    * wenn man sich innerhalb der Gänsefüßchen befindet, sollte automatisch die Tabanzeige kommen (nicht nachdem man bspw. Leertaste und dan Del drückt)
    * Bei Tab, sollte man direkt zu den leeren Gänsefüßchen der nächsten wandern
  * Highlights durchgehen:

    * in Highlights sollten neben Patzer und verpasste Chancen auch enthalten werden -> beste Moves, welche laut Engine der einzige Zug ist welche bewertungserhaltend ist, der letzte Lektionsmove, Endstellung
    * Gib eine vordefinierte Beschreibung an für die verschiedenen Züge (z.B. "Dieser Zug war die einzige Möglichkeit deinen Vorteil zu behalten.")
    * bei Highlights durchgehen, startet man bei der Startposition und gehe die Animationen durch bis zum Highlight
    * Füge neben besten Zug zeigen und Selbstverbessern auch noch Pfeile ein um zum nächsten Zug zu gelangen
    * Zeige den gehighlighteten Zug mit der Animation in voller länge an (also nicht den vorherigen Zug anzeigen lassen), markiere welcher Zug gemacht wurde -> die Knöpfe "Besten Zug anzeigen" oder "Selber verbessern", würden also im ersten Schritt diesen getätigten Zug zurücksetzen und dann ihre jeweilige Aktion ausführen
    * Füge für jede Bewertung (Patzer, Verpasste Chance, ...) ein Icon hinzu und zeige es während der Analyse oben rechts an
* Freies Spiel:

  * gerade erscheinen 2 Fenster auf einmal beim öffnen dieses Modus -> nur Brett aufstellen Fenster öffnen und nach Betätigen des Knopfes "Mit dieser Stellung spielen" wandelt sich die UI dieses Fensters zum Freies Spiel - Unabhängig von Opening Fenster
  * bei Brett aufstellen, sollte stärker markiert werden, dass der Verschiebenmodus aktiv ist, beim Verschieben sollte sich auch die Figur bei Drag and Drop mit bewegen je nach Position des Mauszeigers (wie auch beim normalen Spiel)
  * mehr als Bauern+Queens=9 sind nicht möglich -> wenn das wegen der Engine so ist, dann sollte beim Aufstellen angezeigt werden, dass die Engine nicht funktionieren wird, sonst Bug einfach fixen
* Schach zu zwei:

  * anonymes Spiel Button -> Spiel wird auch nicht gespeichert
  * nach Ende des Spiels -> Spiel Analysieren Button (im Hintergrund auch die Analysen vorbereiten, damit man nicht warten muss)
* Neuer Modus: Schachblick:

  * 3 Submodi:

    * Man muss so schnell wie möglich die Figuren markieren (mit links oder rechtsklick egal), welche diese Position/Figur angreifen bzw. verteidigen -> mit Leertaste bestätigen (Fehler werden gezählt bei Fehlen von Figuren beim bestätigen oder beim markieren von falschen)
    * Man muss so schnell wie möglich alle Figuren markieren, welche gerade eine Position/Figur noch nicht angreifen bzw. verteidigen aber im unmittelbar nächsten Zug es könnten entweder durch Umpositionierung durch diese oder durch eine Fremde Figur
    * Man muss so schnell wie möglich alle unmittelbaren Angriffe durch gegnerische Figuren auf die eigenen markieren (ziehen von Pfeile von Angreifer zu eigene Figur -> dabei sollen markierte Pfeile, welche in der Leere starten oder in die Leere gehen automatisch nicht gemalt werden)
  * Für das finden von Positionen, sollte man nur jene nehmen, zu welchen Leichtfiguren, Türme oder Damen von einem selber oder vom Gegner hingehen könnten und dabei gegnerische Figuren attackieren
  * Dabei beendet sich ein Modus nach 30 dieser Übungen und es wird ein Score berechnet aus richtige Markierungen pro Sekunde -> Speichere jeweils als Highscore"
* 25.09. 8:27: "Arbeite daran weiter, was du nicht beenden konntest"
* 25.09. 13:44: "Arbeite daran weiter, was du nicht beenden konntest"
* 27.09. 22:33: "Arbeite daran weiter, was du nicht beenden konntest, indem du V12 abschließt. (keine Zwischenstände mehr)"
* 28.09. 8:13: "Arbeite daran weiter, was du nicht beenden konntest"
* 29.09. 0:57: "Ändere folgendes:
* Allgemein:

  * Die Anzeige, wer gerade dran ist ist noch nicht ganz gut: Ich habe mir das so vorgestellt, dass die grobe Form ist, dass sich jeweils zwei Linien um die Ecken bilden. Diese beiden Linien sind miteinander Verbunden und bilden praktich eine größere Linie. Die Farbe sagt aus wer dran ist, aber die Linien vergrößeren sich auch auf der entsprechenden Seite, wenn die Seite gerade dran ist.
  * Implementiere noch in den Optionen der Stellungsbewertung die Zeiteinschätzung, über ein reales Beispiel, wie lange das dauern kann.
* Branchmodus:

  * Der Pfeil zum ein und ausklappen der Knoten hat folgende Probleme:

    * Es wird nicht kontrolliert, wie viele Züge tatsächlich im Knoten angezeigt werden -> Wenn ich gleiche Antworten bündle, kann es sein, dass ein einklapppfeil auftaucht, obwohl es nur einen Wert im Knoten gibt
    * Es sollten strickt nur Einklappfeile auftauchen, wenn sie auch die tatsächliche Größe der Knoten reduzieren bspw. ist es bei 2 Zügen in einem Knoten so, dass sie immernoch in der selben Zeile des Knoten geschrieben werden -> dort sollte kein Einklappfeil auftauchen
    * die Pfeile sollten von der Höhe kleiner sein, aber die ganze Breite des Knoten einnehmen (Subbranchpfeile ausgeschlossen)
  * Die Subbranches haben folgende Probleme:

    * Der Pfeil sollte auch die selber Höhe einnehmen, wie der Knoten, aber dünner sein
    * Beim auf und zu klappen verändert sich die gefühlte Kameraposition sehr stark, da ich dir glaube, dass du Kameraposition nicht zurücksetzt -> mache doch lieber beim schließen/öffnen eines Subbranches die Kameraposition an den Offset zwischen Kamera und ersten Knoten im Branchsystem + Position erster Knoten -> so gibt es immer nen Pivot an dem sich die Kamera orientiert
    * Veränder die Farbe der Umrandungen, der sich im Subbranch befindlichen Knoten, sondass jene Knoten, welche zu einem Subbranch gehören die selbe Umrandungsfarbe haben -> ja, ab jetzt sollten auch die Knoten unterhalb eines subbranches auch eine Umrandung bekommen \& die Umrandungsfarbe, von tieferliegenden Subbranches (, wenn man einen Subbranch in Subbranch hat, ) hat Priorität
  * Füge bei der Vorschau des Zuges im Spielfeld (oben rechts) eine Markierung hinzu, was der letzte Zug war
  * Entferne auf der rechten Spalte die Knöpfe: "Diesen Subbranch auf-/zuklappen" \& "Stellungsvarianten vergrößeren/verkleinern" -> Subbranches einklappen wird umbenannt zu "Alles Subbranches einklappen" -> dieser soll aber zu dem selben Knopftypen werden, wie der Verschiebenknopf in Freies Spiel - Brett aufstellen (bei nochmaligen drücken werden die zuvor noch aufgeklappten Subbranches wieder aufgeklappt, aber bei aufklappen eines Subbranches manuell, wird der Knopf auch deaktiviert)
  * Entferne diese Lila/pinke Umrandung für die Engine-untypischen Züge, und behalte nur noch die Umfärbung der Schrift spezifisch
  * Gebe der rechten Spalte (also da wo das Brett zu sehen ist und die Knöpfe) nach links ein paar wenige Pixel mehr Platz, weil es sieht fast so aus, als würden die Knöpfe herausrangen
  * Ordne die Knopfreihenfolge der rechten Spalte anders:

    * Board + zugehörige Infos
    * Stellungsvarianten
    * Lernprioritätseinstellung (In einer Zeile Lernpriorität + das bisherige aufklappfenster aktiv/später/ausgeschlossen)
    * Subbranch erstellen/entfernen -> Knopf je nach Knotenstatus ändern
    * Jetzt diesen Branch lernen
    * Tiefer erforschen
    * alle Subbranches einklappen
    * Human Moves neu generieren
    * Mit anderen Opening kombinieren
  * Verschiebe Engine-untypische Züge ausblenden und Gleiche Antworten als Varianten bündeln zu Ähnliche Gegenvarianten als gemeinsame Lernkarte
  * Blende die gesamt Lernkartenzahl an -> auch live update bei Veränderung von "Ähnliche Gegenvarianten als gemeinsame Lernkarte"
* Freeplay (für Opening):

  * wenn außerhalb Opening Knopf mit ab hier weiterspielen
* Lernen:

  * Letzten Zug mit Markierung anzeigen
* Freies Spiel:

  * Option einfügen für Schwirigkeitseinstellung von Engine (bspw. wie siel % seiner Moves sollen beste Moves sein)
  * Zurückknopf einfügen für die Stellungsbearbeitung, wie bei den Rätselmodi -> wenn man mind einen Zug gespielt halt, sollte ein Pop Up kommen, ob man sicher ist, dass man verlassen möchte
* Opening Branch Generator: nach dem Schließen des Tabs, wird das gesamte Menü neu gemalt
* Rätsel::

  * Bei einfachem Bauernendspieltraining Festung/Remis halten sollte die Engine versuchen den Benutzer auf die Probe zu stellen, indem nicht immer dieselbe Taktik benutzt wird, indem er erst den Bauern vorsetzt, dann mit dem König nach rechts geht, und immer wieder -> wie soll man die Stellung halten, wenn der König mal wartet, oder nach links geht?
  * erstelle für die Rätselsubmodi eigene Piktogramme
  * Remis in 3 Zügen war für mich eine Verarsche, weil die Evaluation am Ende sagt M11, weil ich noch einen Turm übrig hatte -> generell sind die Remis sehr umkreativ -> bisher nur eine schwere Figur nehmen, oder Eine Gabel machen, aber das geht doch bestimmt auch noch besser
  * Entferne aus Matt in N die Endspielkompositionen -> nur noch Komposition und v.a. Engine-Selbstspiel
  * Wenn ich einen neuen Submodus auswähle, erscheint noch der zuvorausgewälte Modus für einen Frame
  * Ich warte die ganze Zeit auf Nachschub, aber es kommt kein Taktikaufgaben Nachschub mehr -> im Taskmanager ist die CPU Leistung der stockfish-windows-x86-64-universal.exe auch bei 0% -> da wird nichts berechnet
* Eigene Spiele \& Analyse:

  * dass beim Hochscrollen immernoch Weiß/Schwarz gemalt wird, wurde immernoch nicht gefixed
* Schachblick:

  * Sounds für Erfolg und Misserfolg fehlen
  * Fehler auch beim Score anzeigen lassen -> Fehleranzahl hat Vorrang, bei gleicher Zahl, dann die Figuren/Zeit
  * das was man nicht markiert hat bei Fehler gesondert anzeigen lassen und man kann mit einem zweiten drücken auf die Leertaste zum nächsten
  * implementiere ein Pausenknopf mit einer Anzeige vor dem Brett, sodass man nicht schummeln kann
  * indirekte Angriffe/Verteidigung sollte mit einbeziehen können (bspw. wenn es eine Reihe von Läufer und Dame zum Target gibt, dann sollte die Dame miteinbezogen werden, weil ja der Läufer bei einer Verteidigung den Weg frei machen würde)
  * Vor dem Beginn, sollten die Regel groß erklärt werden je nach Modus, und es sollte erst nach einer Bestätigung mit Leertaste beginnen
  * Die Farben sollten sich nach jedem Board abwechseln (also weiß, dann schwarz, dann wieder weiß, dann schwarz, ...) -> damit man weiß, auf welche Farbe man wie schauen soll
  * Neu: Erstelle eine anzeigbare Statistik, sodass man sehen kann, welche Typen von Moves, von einem beim jeweiligen Modus häufiger übersehen wurden
  * Neu: füge weitere Submodi ein:

    * angegriffene Figur plazieren, wo sie nicht angreifbar ist
    * Boardtinder (umbenennen): aus einer Stellung, werden zwei mögliche Moves angezeigt, und man soll selber sagen, welcher der beiden Moves besser ist -> eher tiefe Enginekontrolle, \~0,5 oder mehr Evaluationsunterschied anpeilen -> auswählbar mit Maus, direkt das bessere Board, davor aber eine Animation aus der Sicht des originalboardes aus -> mit Möglichkeit zwischen vorheriger Stellung und angebotener STellung gleichzeitig bei beiden zu switchen
    * Figuren merken (\~10s Zeit), und man muss die Stellung wieder aufbauen -> orientiere dich an dem Custom Game Brett aufbauen Editor"
* 29.09. 8:23: "Arbeite daran weiter, was du nicht beenden konntest"
* 02.10. 2:04: "Ändere folgendes:
* Analyse:

  * Füge die Piktogramme für jede Bewertung der Züge ein, bei der Analyse von eigenen Spielen als kleine Anzeige oben rechts von der bewegten Figur (ähnlich wie schon bei Remis, auch für Bester Zug, Verpasste Chance, Matt, ...) -> orientiere dich dabei an den Piktogrammen, die schon in der rechten Spalte benutzt, aber auch in Farbe
  * Beim Durchschwitchen durch die Züge mit Pfeiltasten wird die Markierung nicht mehr angezeigt.
* Rätsel:

  * Lass konkret im Debug-Modus anzeigen inwieweit gerade neue Rätsel generiert werden (wie viele Arbeiter, wie viel wurde schon abgelehnt, ...), statt die einfache Nachricht "Für diese Taktikfilter ist noch kein vorbereitetes Rätsel vorhanden. Andere Schwierigkeit wählen oder auf Nachschub warten" -> füge stattdessen eine Ladeanzeige an auf dem Board, dass neue Rätsel generiert werden (+ angesprochener Debug)
  * Ist es normal, dass ich im Taskmanager nicht die benutzten Engines bzw. Prozesse sehe, bei der Generierung von Rätseln -> kann ich nur anhand der CPU Nutzung sehen
  * Es dauert zu Lage, um leichte Rätsel für Matt in N zu generieren, obwohl die ganze Zeit irgendetwas berechnet wird (ich kann mir vorstellen, dass du die Requirenments zu stark gesetzt hast, sodass die Wahrscheinlichkeit zu gering ist)
  * (Kontrolliere nochmal, ob die Generierung neuer auch wirklich während der Bearbeitung der im Vorrat befindlichen Rätsel passiert)
  * Zeige bei den Mittelspieltaktiken die Enginevarianten aller akzeptierten Züge, und den nächstbesten
  * Entferne den Modus Remis sichern, da findet sich nämlich scheinbar zu wenig Variation
* Branchmodus:

  * Die Umrandungen für die Markeirung der Subbranches, sollten die Pfeile zum ein und ausklappen entweder des Knoten oder des Branches mitumfassen -> beim Auswählen eines Knoten genauso
  * Die Auswahl eines Knoten sollte aufgehoben werden, wenn man ins leere klickt (neben einen Knoten)
  * Füge eine aufklappbare linke Spalte ein unter den Knöpfen Gesamter Graph, Aktuelle Stellung \& Alle Subbranches einklappen (selbst bei eingeklappter Apalte immernoch vorhanden). Verschiebe folgende Einstellungen dort hin in folgender Reihenfolge:

    * Ähnliche Gegenvarianten als gemeinsame Lernkarte
    * Gleiche Antworten als Varianten bündeln
    * Engine-untypische Züge ausblenden
    * Engines-untypische Züge neu generieren (ersetzt Human Moves neu generieren)
    * Mit anderem Opening kombinieren
* Schachblick:

  * Zurückbutton einfügen bei der Erklärung des jeweiligen Modus.
  * Füge eine Rote Markierung für die falsch ausgewählten Positionen/Figuren
  * Mach die Statistiken übersichtlicher mit Säulendiagramme für jeden Modus
  * Füge ein Abbruchknopf für die Modi hinzu
  * Figuren retten für Figuren, welche sich nur an einem Ort bewegen können (bspw. Bauer, welcher nach vorne geht) oder wo alle Möglichkeiten akzeptiert sind ist witzlos
  * Figuren retten: Füge optionale Einstellung hinzu, dass man zwar immernochdie betroffene Figur markiert sieht, aber die Position wo sie hingehen kann nicht mehr.
  * Bei Zugvergleich taucht nach jedem Rätsel "Aufgabe wird vorbereitet" auf -> alles sollte doch schon wie beim Rest wenigstens für einen Run vorgeneriert sein -> ich hoffe es findet generell bei allen Modi eine weitere generierung statt, damit man nach ner Zeit immerwieder diesselben Aufgaben sieht (ähnlich wie beim Rätselmodus)
  * Ordne den Figurenmerkenmodus in leicht, mittel, schwer (je nach Figurenzahl)"
* 02.10. 10:47: "Arbeite daran weiter, was du nicht beenden konntest"
* 07.10. 18:02: "Ändere folgendes:
* Allgemein:

  * Eckenanzeige am Board (damit man weiß wer dran ist):

    * Mach bei der größeren Ecke auch die Liniendicke leicht größer
    * Wenn die Ecken schwarz sind, gib denen noch eine größere weiße Umrandung, sodass sie sich leichter vom Hintergrund abheben (bei den weißen Ecken genauso für schwarze Umrandung, fällt nur nicht so krass auf, weil der Hintergrund schwarz ist)
    * Füge bei den Ecken eine Übergangsanimation ein, welche so lange wie die Zuganimation braucht, wobei sich die Farbe doppelt so schnell wie die Zuganimation verändern soll.
  * Figurendarstellung bei der Anzeige, zu den zuvor gespielten Züge: Ich hatte dir ja vorher gesagt, dass die Umrandung der weißen Figuren auch weiß sein soll, wie bei den schwarzen Figuren, damit beide Figuren gleich dick wirken. Verändere doch diese Umrandung für die weißen Figuren auf schwarz.
  * Enginevarianten-Anzeige: Verändere das Verhalten des Sliders. Bisher orientiert sich die mittlere Position des verschiebe Kastens an der Position des Mauszeigers. Ändere es so, dass auch der Offset zwischen horizontale Mauszeigerposition und Mittlere Kastenposition mit einberechnet wird, sodass sich die Position des Kastens nicht direkt nach dem Mausdruck flipped, sondern eine Verschiebung mit der Maus ist auch noch vorher notwendig -> ähnlich wie bei den anderen schon benutzten Slidern.
  * Zugverlauf-Anzeige: Ändere den Slider visuell im Stil von der Enginevarianten-Anzeige.
  * Der Zur aktuellen Stellung Knopf, sollte in jedem betroffenem Modus nur angezeigt werden, wenn sich der Benutzer nicht auf der aktuellen Stellung befindet. Der Knopf sollte unter der Zug-Historie Anzeige groß auftauchen.
  * Füge für Brett drehen ein eigenes Piktogramm ein und passe es entsprechend bei den betroffenen Modi an.
  * Gibt dem Turm beim App-Icon eine stärkere Schwarz Umrandung, sodass man den Turm auch auf der oberen Zeile der Fenster sehen kann.
  * Füge die Möglichkeit ein seine Openings im Hauptmenü umbenennen zu können. (bei Doppelklick auf dem Namen mit Bestätigen über Enter-Taste oder über aufploppenden Knopf).
* Customization:

  * Mach in den Einstellungen es möglich die Dicke der Pfeilmarkierung, die Farbe der Pfeilmarkierung und die Farbe der Auswahlmarkierung zu ändern mit entsprechende Preview.
* Eigene Spiele \& Analysen:

  * In deinem Output Ordner findest du selbst gemachte Visuals für die Icons für die Bewertung der Züge (in Bester Zug, Guter Zug, Schlechter Zug, Patzer, Verpasste Chance, ...). Diese sollst du statt deinen bisherigen verwenden -> passe bei der rechten Spalte an, wo auch nochmal eigene Icons verwendet werden das so an, dass dieselben neuen Icons verwendet werden.
  * Bug: Wenn ich "Nächster Zug" so schnell spame (bei vorheriger Zug funktioniert es wie gewollt), dass die Animation der gespielten Figur nicht beendet werden kann, dann wird nicht zur nächsten Stellung gesprungen, sondern die vorherige Stellung verbleibt. Ich erwarte, dass beim mehrmaligen tippen von Nächster Zug die aktuelle Stellung direkt gespeichert wird, und statt einen Abbruch, dass die Figur, zu seiner rechtmäßigen Position springt und dann die Animation der sich als nächsten bewegenden Figur abgespielt wird.
  * Verändere das Layout:

    * Eigene Spiele Anzeige:

      * Behalte zwar die Reihenfolge der Matches wie jetzt (also Sortierung nach Zeit, neuste zuerst), nur ändere dann wieder das Datum auf die richtige Ordnung (Tag.Monat.Jahr(ohne Suffix 20 oder 19))
      * Zur Ordnung schaffe pro Zeile 3 Spalten:

        * Die erste Spalte ist für das Datum, die zweite für den Namen des Spielers mit weiß und die dritte Spalte für jenen mit schwarz.
        * Das Datum wird mit einem vertikalem, die ganze Zeile hohen Strich von den beiden Spielernamen getrennt.
        * Die Spalten für die Spielernamen sind unabhängig von der länge der Spielernamen (also konstant)
        * Spalte 2/3 kann von dem bereits bestehendem horizontalem Strich getrennt sein.
        * Wer gewonnen hat wird dadurch angezeigt, dass der Gewinner mit einer dickeren Schrift geschrieben wird, während der Verlierer dünner und eine blassere Schriftfarbe hat. Bei Remis, sind beide blass und dünn.
      * Orientiere dich für die Auswahl, welches Match du ausgewählt hast daran, an die Anzeige welche Züge vorher gespielt wurden (also ähnliches Hovering verhalten und bei Auswahl, sollte die Zeile nicht blau werden, sondern blasser)
    * Brett: Füge die Spielernamen mittig am oberen und unteren Rand des Boardes hinzu.
    * Highlights durchgehen:

      * Verschiebe den Highlights durchgehen Knopf auf auf der rechten Spalte oben, wenn man ein Spiel ausgewählt hat, wenn das Spiel sich in der Startstellung befindet. Mach den Knopf extra groß und mittig.
      * Entferne den Zurückknopf (für den letzten Zug) bei der Startstellung.
      * Gehe die Highlights nach der Boarddrehung durch (aus Sicht des Spielers, aus dessen Sicht man von unten guckt). Verändere entsprechend mitten in der Analyse die Funktionalität des "Nächster Highlightzug"-Knopfes, dass er immer aus der Sicht der Boarddrehung ist.
      * Wenn das Spiel nicht in der Startposition ist, füge zwischen dem vorheriger Zug- \& nächster Zug-Knopf einen "Nächster Highlightzug"-Knopf hinzu. Der Highlights durchgehenknopf ist nirgends mehr vorzufinden, außer man gelangt wieder zu der Startstellung.
    * Analyse-Einstellungen:

      * Der Knopf sollte direkt zum Partie-Analyse-Untertab der Einstellungen führen und nicht nur das Einstellungsmenü öffnen.
      * Verschiebe ihnen auf den rechten Bildschirmrand und benutze als Inhalt dasselbe Piktogramm wie im Hauptmenü.
    * Standardspieler-Anzeige:

      * Diese Anzeige befindet sich ja über die Eigene Spiele-Anzeige und wählt als Standard den häufigsten Spielernamen aus. Verschiebe diese Anzeige am oberen Bildschirmrand mittig (neben dem Fehlende Partien analysieren Knopf -> wenn möglich aber mit anstand, also abhängig von Fenstergröße) ein mit dem dem Präfix "Standardspieler: " (also vor dieser Anzeige)
      * Füge eine Typefunktion ein, in welchem alle Spielernamen aus Eigene Spiele mit dem eingetippten verglichen werden, wenn nichts ausgewählt wird (auswählen entweder durch Mausklick oder durch Enter mit Auswahl über Pfeiltasten oben und unten; nicht Auswahl durch Mausklick oder ähnliches außerhalb der Anzeige), dann das zuvor ausgewählte beibehalten.
* Custom Opening Generator (COG):

  * Ändere die Farbe der Input Kästen, sodass die Ziffern und Buchstaben in weiß sind auf dunklem Hintergrund. -> soll über die Stile einstellbar sein (füge nicht eine seperate Einstellung hinzu für die Input Kästen des COGs, sondern benutze im Stil schon bestehende Anpassungsoption für eine Farbauswahl)
  * Füge eine Preview hinzu, als eigenständiges Fenster, in welchem man das wachsen des erstellten Branches sehen kann -> ohne Konkrete Züge nur Knoten und Verbindungen, ohne Zoom-Funktion (ganzer Graph auf einmal sichtbar) mit einer Progressbar, die zumindest halb akkurat ist (mit entsprechender Prediction wie viele Karten pro Tiefe anhand dessen den Fortschritt normieren, die Fortschrittsanzeige, darf sich nie nach hinten korrigieren)
* Hauptmenü:

  * ändere den rechten Slider auch im Stil von der Enginevariantenanzeige.
* Rätsel-Modus:

  * Taktik:

    * Füge beim Hovern für jede Einstellung Informationen hinzu (unteranderem stehen schon im Titel der Einstellungen zusätzliche Informationen in Klammern bspw. "(Sekunden, 0 = ohne Limit)" -> diese einfach beim Hovern integrieren).
    * Verschiebe bei jeder Einstellung, bei welcher es entweder eine Auswahl von vordefinierten Sachen existieren oder man nur eine Zahl verändern soll diese rechts neben dem Namen des Parameters, statt wie bisher dadrunter.
  * Verändere die Farben der Input-Kästen bei jedem Untermodus ähnlich zu jenem beim COG.
  * Bei Endspieltraining "Bauern Durchbruch \& Verteidigung" wird das \& nicht angezeigt
* Freies Spiel:

  * Brett aufstellen: Bei der Figurenauswahl sollte bei der ausgewählten Figur dieselbe Hintergrundfarbe benutzt werden wie für den Verschieben-Knopf.
  * Verändere das Layout beim eigentlichen Freies Spiel-Modus:

    * Orientiere dich genau an dem Rätselmodus:

      * es sollte statt "Training" im Rätselmodus "Freies Spiel" als Überschrift genutzt werden
      * Links daneben soll auch der Zurück-Knopf im selben Stil angezeigt werden wie im Rätselmodus
      * Die Knöpfe "Brett drehen", "Zug zurück" \& "Neue Partie" sollen in der Reihenfolge direkt unter der Unterschrift/Zurück-Knopf sein (wie beim Rätsel die Knöpfe "Nächstes Rätsel", "Lösung zeigen", ...)
    * Füge auf die Rechte Spalte von oben nach unten die folgenden übrigen Einstellungen "Bewertung", "Top 3", "Engine spielt den Gegner" (alle Standard false), "Engine-Suchtiefe" (beim Hovern entsprechende Info), "Engine-Intelligenz" (ersetzt "Beste Züge %") (Anzeige nimmt nur eine Zeile ein, in % als Schieberegler oder manuell eintippbar, füge beim Hovern entsprechende Info ein)
    * Ton-Einstellung wird entfernt.
* Lokales Spiel:

  * Ändere das Layout:

    * Füge einen zurück Knopf ein, ähnlich wie beim Rätselmodus (aber ohne Überschrift), nur hier mit extra Abfrage, ob man sicher ist, dass man das Spiel verlassen möchte (außer das Spiel ist schon beendet, dann keine Abfrage mehr)
    * Füge die Zeitanzeige ähnlich groß in der rechten unteren und oberen Ecke des Boardes ein rechts von der rechten Seite des Boardes. Die Zughistory-Anzeige. Ist entsprechend im kleineren Bereich der rechten Spalte, sodass jene nicht die Uhren überlappen.
    * Auch hier Zur aktuellen Stellung Knopf anpassen, wie bei den anderen betroffenen Modi
    * Die Knöpfe Remis vereinbaren und Aufgeben, bekommen eigene Piktogramme in der rechten Spalte unten rechts, sodass es auf der gleichen Ebene ist wie die untere Uhr
    * Uhr pausieren/fortsetzen, sollte seinen Namen ändern entsprechend der zu tätigen Aktion. Verschiebe den Knopf auf die rechte Spalte oben rechts, sodass die obere Uhr auf der selben Ebene ist
    * Das Brett drehen Piktogramm sollte oben rechts vom Brett auch an der rechten Kante des Brettes sein, aber über die obere Uhr.
    * Oben sollte also nur noch ein Zurückknopf auf der oberen Leiste zu sehen sein.
  * Remis vereinbaren und Aufgeben Piktogramme sollten verschwinden, wenn noch in der Startstellung oder das Spiel entschieden ist. Bei letzterem füge unter die Zug-History einen großen Knopf ein: "Neues Spiel" (gleiche Einstellungen werden benutzt für das nächste Spiel).
  * Füge auch hier, wenn gegeben die Namen der Spieler ein.
* Schachblick:

  * Wenn man Schachblick öffnet taucht unten ein leeres Board auf. entferne dieses und mach die Modiauswahl, wie beim Rätselmodus (jeder Submodusknopf in einer Spalte übereinander).
  * Behandle den Statistikmodus, so wie einen normalen Submodus, aber gib dem einen zusätzlichen kleinen vertikalen Abstand zu den anderen richtigen Submodi.
  * Füge oben rechts über das Einstellungspiktogramm die bestehenden beiden Einstellungen ein.
  * Statistik:

    * Füge die Figurenpiktogramme ein, statt B - Linienangriff.
    * Füge die Merken-Tabs zusammen, sodass man innerhalb des Merken-Tabs die Statistik einblenden kann für leicht, mittel, schwer.
    * Zeige die Statistik als Säulenzusammensetzung über Zeit ein:

      * Man kann die betrachtete Zeit einstellen pro Säule (pro Tag, pro 3 Tage, pro Woche, pro Monat).
      * Die Fehlerzusammensetzung wird in % angezeigt, sodass jede Säule bei 100% landet, aber unterschiedliche Farbenzusammensetzung hat, je nach Fehlertyp.
      * Auch eine Anzeigen für: Durchschnittliche Fehlerzahl/Durchgang über Zeit, Durchgangsanzahl über Zeit, Gesamte Durchgänge, Gesamte Fehler, Durchschnittliche Geschwindigkeit über Zeit
  * Der Zurückknopf, sollte so sein, wie bei dem Rätselmodus.
  * Kein Schachblicksubmodus, sollte ein neues Fenster erstellen, sondern alle sollten im selben Fenster bleiben. -> Das gilt nicht für die letzten 3 Modi.
  * Bei der Modusbeschreibung, bevor man startet, sollte das Board im Hintergrund an den Rändern noch zu sehen sein, während der Hauptteil von dem Modusbeschreibungskasten verdeckt wird, und erst nach Bestätigung verschwindet. (Bisher ist ja das Board gar nicht ersichtlich).
  * Bei falscher Auswahl, sollte die rote Markierung erst nach der Bestätigung auftauchen.
* Branchmodus:

  * Bei Klick auf aktuelle Stellung, sollte der betroffene Knoten ausgewählt werden
  * Die Einklappmöglichkeit der linken Spalte, sollte so wie beim ein und ausklappen eines Subbranches sein. Der Einklappknopf befindet sich also auf der gesamten rechten Kante der linken Spalte. Bei Klick, verschwindet die linke Spalte und es bleibt derselbe Knopf übrig, nur dass er nun eine Ausklappfunktion hat.
  * Mach die Subbranchfarbe einstellbar über ein Colorwheel nach dem Drücken eines Quadratischen Kastens in der rechten Spalte, welcher die Farbe des Subbranches besitzt über den Stellungsvarianten Parameter rechts.
  * Schreibe Stellungsvariante + Einstellungsparameter auf dieselbe Zeile -> wie du es auch bei Lernpriorität gemacht hast.
* Lernmodus:

  * Die Progressanzeige für den Anteil der gelernten Karten ist entweder falsch oder unklar. Im Vollbild ist die Anzeige \~ halb so groß, wie sie sein müsste, um den tatsächlichen Anteil an angeschauten Karten darzustellen. Im Fenstermodus, wird die Progressbar, wahrscheinlich durch einen unsichtbaren schwarzen Katen auf der links unteren Ecke überlappt. Ich gehe davon aus, dass im Vollbild die Anzeige in 2 separate Abschnitte geteilt ist, design das lieber so, dass jeder Kartenstatus innerhalb der selber Leiste angezeigt wird mit ihren entsprechenden Anteilen (wenn 0, dann entsprechend gar nicht wie bisher)."
* 08.10. 0:54: "Arbeite daran weiter, was du nicht beenden konntest"
* 10.10. 18:12: "Ändere folgendes:
* Allgemein:

  * Bei den Input Kästen, wenn Pfeile auch dabei sind (entweder zum öffnen des Dropdown-Menüs oder zum vergrößern von des int/floats) diese auch umfärben (-> bisher noch weiß, Kontrastreduktion).
  * Jedes Fenster hat ja seinen eigenen weißen Balken oben, um anzuzeigen, wie das Fenster heißt und minimierung, schließen, ... Buttons. Verändere die Farbe dieses Balkens dunkler.
  * Neu: Füge einen White-Stil für die App hinzu.
  * Die Umrandung bei den Ecken des Boardes, welche anzeigt, wer gerade dran ist, sollte zumindest bei Schwarz (bei weiß wahrscheinlich auch) auch um die Enden der Linien gehen (also v.a. bei der schwarzen Linie mit der weißen Umrandung sieht man es besonder gut, dass die Umrandung nicht um die Enden der Linien geht).
  * Beim Umstellen von Farben, sollte in einem seperaten Thread die Farbe live geupdatet werden (beim Branchmodus und bei den Markierungen).
  * Mach es auch möglich, die Farbe zu ändern der Markierung beim einmaligen Linksklick (seperat von der Pfeilfarbe).
  * Mach es auch möglich die größe der Pfeilspitze zu ändern, nicht nur die Pfeildicke.
* COG:

  * Die Schätzung des wachsenden Opening-Branch ist echt nicht gut, nach der Anzeige der Startstellung, steht da schon, dass man einen Fortschritt von 30,5% hat, während die Tiefe bei 25 ist -> mach auch eine Zeitprognose anhand der Dauer der letzten Generierungen + prognostizierte fehlende Karten (v.a. passt der Ladebalken sich nicht der vorher prognostizierten Zeit am Anfang an -> die Prediction sollte am Anfang genauso sein wie der Kartenzahl neu schätzen Button)
  * Der Wachsende Opening-Branch-Tab, sollte von der Minimierung und Fensterpriorität des COGs unabhängig sein.
  * Bei der Wachsendem Opening-Branch, sollte mit Zunehmender Branchgröße (horizontal) die Länge Knotenkette vom Start bis zur ersten Abzweigung abnehmen
  * Zeige im wachsenden Opening-Branch auch die Engine-Tiefe an (auch von wieviel)
* Eigene Spiele \& Analyse:

  * Gespielte Spiele Anzeige:

    * Beim Hovern über die Matches fängt es an zu flackern. Passiert immer, wenn ich mit der Maus über ein anderes Match bin. -> ich gehe davon aus, dass alles neu gemalt wird, wenn sich nur die Auswahl verändert
    * Wenn man durch die Matches Scrollt, ändert sich das Hovern nur, wenn man die Maus bewegt und nicht live beim Scrollen.
    * Du hast vergessen auch hier den Slider anzupassen, dass er ähnlich zu der Zug-History bzw. der Engine-Varianten ist.
    * Die Höhe pro Zeile ist zu groß \& Die Datumsspalte nimmt auch zu viel Platz ein
  * Beim PGN Schreiben, sollte sich der Katen für die Empfehlungen auch entsprechend des Stiles färben (bisher nur weiß)
  * Bei einer meiner getesteten Analysen taucht mehrmals auf "Hier endet die bekannte Opening-Theorie dieser Partie" mit einem Custom Icon -> entferne dieses Icon \& die Möglichkeit, dass ein Zug als Theoriezug erkannt werden kann.
  * Wenn man durch die Zug-Historie mit den Pfeiltasten durchgeht oder entsprechend das nächste auswählt mit einem Mausklick, dann sollte auch eine Animation gespielt werden
  * Wenn man diesen Nächsten Zug Knopf durchspammt, sollte nicht erst gewartet werden bis man den Knopf fertig durchgespammt habe und dann auf die richtige Stellung springen, sondern wie folgt (erster Input -> Versuch Animation wie üblich komplett durchzugehen; zweiter Input -> Setze Board auf die eigentliche Stellung nach beenden der vorherigen Animation und fange entsprechend die neue Animation an für den nächsten Zug; dritter Input -> siehe zweiter Input; vierter Input -> siehe zweiter Input; ...).
  * Der Nächster Highlightzug Knopf, sollte ähnlich groß sein, wie der jetzige Highliths durchgehen Knopf. Der Highliths durchgehen Knopf, kann von der Höhe größer sein. Die Knöpfe Nächster Zug, Vorheriger Zug, Besten Zug zeigen \& Selbst verbessern, sollten alle in einer Zeile unter Nächster Highlithzug sein, aber die letzten beiden zwischen den ersten beiden erwähnten Knöpfe mit leicht geringerer Knopf-Höhe als  Nächster Highlightzug
  * Bei Standardspieler, sollte der Slider auch angepasst werden und auch bei den Einstellungen.
* Schachblick:

  * Die obere Beschreibung ist über den ersten Knopf und über einen Teil des zweiten Knopfes, sodass man die Knöpfe im entsprechenden Bereich nicht mehr anklicken kann.
  * Mach den Backpfeil so wie im Rätselmodus, also nicht die ganze obere Zeile ennehmenden, sondern nur oben links (mit kleinem Abstand zum oberen und linken Fensterrand)
  * Statistik:

    * Der Statistikbereich, sollte einen eigenen Tab erstellen.
    * Bei der Fehlerzusammensetzung sollte die Legende links vom Diagramm integriert sein (entsprechend auf der jeweiligen Höhe, in welcher der Fehler vorkommt) bzw. bei genügend Platz integriere den Fehler direkt im Diagramm.
    * Füge auch noch die Einstellung ein, dass man den Zeitraum einstellen kann.
    * Füge für mich zum testen pseudowerte ein, für die letzten 400 Tage (an manchen Tagen/Wochen/Monate mal aktiver, mal nicht (max 10Durchgänge/Modus/Tag mit durchschnitt von 2/Modus/Tag mit Phasen wo mal intensiver gespielt wurde, und wann nicht), mit Veränderung der Fehlerzusammensetzung und generell mit Verbesserung der Durchschnittsgeschwindigkeit/Fehlerzahl wobei nach Passagen wo weniger bis gar nicht gespielt wurde verschlechterung festzustellen ist) -> Die Häufigkeit der gespielten Durchgänge sollte für jeden Modus korrelieren.
    * Sonstige Fehler sollte nicht existieren. -> Benenne Exakt, was das für ein Fehler war
* Freies Spiel:

  * Bei einer Engine-Intelligenz von 0%, sollte die Engine immer den schlechtesten Zug spielen; bei 50% mehr oder weniger von den spielbaren Zügen einen Random Zug.
* Branchmodus:

  * Die Farbe soll für den gesamten Branch nicht nur über den Root-Node einstellbar sein, sondern auch über einen Subbranch.
  * Ein Teil der Farbanzeige wird vom übergestellten Textkasten übermalt.
  * Der Abstand zwischen den Parametern Stellungsvarianten \& Lernpriorität ist zu groß, sollte ähnlich groß sein zwischen den Knöpfen Subbranch erstellen und Jetzt diesen Branch lernen zwischeneinander."
* 11.10. 0:54: "Ändere folgendes:
* Allgemein:

  * Beim Umbenennen von seinen Openings, sollte die Umbenennung abgebrochen werden, wenn man mit Linksklick daneben klickt.
  * Entferne in den Einstellungen die Möglichkeit mit dem Mausrad Einstellungen zu ändern.
  * Bei den Einstellungen bei dem Stellungsbewertungstab, geht die Laufzeit an Beispielt messen außerhalb des Fensters. (Entferne einfach den Teil "Beispielmessung, keine Garantie für andere Stellungen".)
* COG:

  * BUG: Die Dauer bis die Anzahl der Tiefenabschnitte ist viel zu lange (\~2s). Während das lädt sieht man sieht man mehrere Reihen von diesen Pfeilen, um die Anzahl der Parameter zu ändern, welche sich umherbewegen.
  * Beim Wachsenden Opening Branch färbe da wo die Knoten zu Ende gegangen sind in rot (statt dem standardmäßigem Grün) und füge nicht noch die Möglichen Stellungsvarianten hinzu (Also Ende bei der eigenen Stellung, aber nur visuell, ändere nicht an der Funktionalität der Brancherstellung).
* Schachblick:

  * Statistik:

    * Ordne bei der Fehlerzusammensetzung die Legende nach Figurenvalorität (ganz oben Bauern, ganz unten König) und passe die Farben so an, dass jede Figur seine eigene Farbe hat, aber bei den verschiedenen Fehlermöglichkeiten pro Figur, soll die Farbe einfach in einer anderen Sätigung o.ä. sein. -> mach die Sättigung pro Fehlertyp dabei einheitlich.
    * Passe die Farben des Kalenders und die unterschiedlichen Tabs pro Submodi (diese obere Zeile, wo man den Submodi auswählt), so an, dass es dem restlichen Stil übereinstimmt (im normalen Stil, erscheint es nämlich noch weiß).
    * Füge die Möglichkeit ein, dass man, wenn man einen der Fehlertypen anklickt, nur noch dieser Fehler im Diagramm erscheint, dabei wird der Balken von unten anfangend angezeigt und endet entsprechend seines Prozentranges. Durch das wiedermalige anklicken eines ausgewählten Fehlers, wird er deselected (wenn kein Fehler selected ist, bedeutet das dasselbe wie, wenn alle selected sind). Es können auch mehrere Fehlertypen selected werden. Die nicht ausgewählten erscheinen dann nur noch ausgegraut. (Standardmäßig alles abgewählt -> also alles angezeigt).
    * Füge eine Option hinzu, dass zwischen den jeweiligen Balken Übergänge generiert werden (also einfach pro Fehlertyp ein Trapez, bei dem die linke Seite entsprechend an der Position ist, wie beim linken Balken und für die rechte Seite entsprechend genauso -> linearer Übergang), sodass es keine Lücken mehr gibt zwischen den Balken.
    * Füge für jede Statistik auch einen Graphmodus hinzu, in welchem jeder Datenpunkt eine Ecke vom Graphen ist (also wieder ein Graph aus mehreren Linien zusammengesetzt). Bei der Fehlerzusammensetzung sieht der Graphmodus hingegen so aus, dass jede Position der Datenpunkte innerhalb einer Spalte/eines Balkens stattdessen unabhängig von den anderen ist (also sagen wir Königsnähe hat 10% und Linienangriff Läufer hat 9%, dann sollte die Könignähe bei der Höhe für 10% liegen und der Läufer bei 9% -> Die Graphen dürfen sich also auch schneiden). Bei Fehler/Durchgang und Geschwindigkeit werden jeweils die Datenpunkte ausgelassen, wo kein Spiel gespielt wurde. -> Wenn man dann nur noch spezifische auswählt, dann erscheint auch noch ein zusätzlicher Graph, welcher alle ausgewälten zusammenrechnet (wenn nur ein Fehlertyp ausgewält oder alle Fehlertypen außer max einer nie aufgetreten ist, dann sollte es diesen zusätzlichen nicht geben).
  * Beim Vollbildmodus, sollte die Infoanzeige immernoch mittig vor dem Board bleiben. (Bisher ist bei Fullscreen nur am linken Bildschirmrand angeheftet.)
  * Passe auch die Farbe des Infokastens an, sodass sie nicht dieselbe ist, wie die Hintergrundfarbe (benutze im Stil die Sekundärfarbe o.ä.).
  * Hauptmenü:

    * Füge die Überschrift hinzu "Schachblick" -> passe das an anhand des Hauptmenüs im Rätselmodus
    * Zentriere die Knöpfe und Überschrift horizontal.
    * Füge für jeden Modus (u.a. für Statistik) ein eigenes Piktogramm ein. -> passe die Position \& Größe der Piktogramme anhand des Hauptmenüs des Rätselmodus an
    * Bei jedem Modus, sollte der Rekord eine Zeile unter dem Namen des Submodus stehen (wobei der Name natürlich größer sein soll) -> bei der Größe der Namen passe das an anhand dem Hauptmenü im Rätselmodus (weil noch zusätzlich der Rekord hinzukommt, darf entsprechend der Knopf von der Höhe nach unten hin größer sein)
    * Mach die Farbe der Knöpfe, so wie beim Rätselmodus Hauptmenü (halt die Sekundärfarbe oder wie du das bei Rätselmodus gemacht hast).
    * Erstelle ein eigenes Einstellungstab für Schachblick und füge die schon vorhandenen Einstellungen ein. -> der Einstellungsknopf im Hauptmenü soll entsprechend dort hinführen
* Rätselmodus:

  * Füge wie beim Schachblickmodus oben rechts in die Ecke einen Einstellungsknopf hinzu -> soll zum Rätseltab der Einstellungen hinführen.
* Lokales Spiel:

  * Wenn noch kein Zug gespielt wurde, sollte nicht die Frage ausploppen "Laufende Partie verlassen?"
  * Pausiere den Timer, wenn Laufende Partie verlassen? angezeigt wird.
  * Füge ein Pausepiktogramm vor dem Spielbrett (am besten mit Einblend- und Ausblendanimation (sehr schnell \~0,25s)), wenn die Uhr pausiert wird.
* Eigene Spiele und Analyse:

  * Bei Doppelklick auf einen Zug, über die Zug-History, sollte direkt zur Position gesprungen werden, ohne die ganzen Animationen laufen zu lassen. Behalte dabei den Zur aktuellen Stellung Knopf bei.
  * Nur wenn man spezifisch auf dem Knopf drückt "Zug zurück", dann sollte nicht die Animation abgespielt werden, was der letzte Zug war, sondern da sollte der zukünftige Zug rückwärts abgespielt werden. Mach dabei die Animationsgeschwindigkeit für den Rückwärtszug doppelt so schnell wie der Vorwärtszug.
  * Wenn man über die Zughistory zur Startstellung gelangt, dann sollte auch hier der Highlights durchgehen Knopf erscheinen (momentan verbleibt Nächster Highlightzug)
  * Die Bewertung des nächsten Zuges soll direkt angezeigt werden, während die Animation zur nächsten Position startet, und nicht wenn sie erst beendet ist (das passiert bspw. nicht bei Nächste Highlightzug oder manuell über die Zug-History -> hier wird die Animation vollständig abgewartet).
  * Verschiebe die Knöpfe für Nächster Highlightzug, Nächster Zug, Vorheriger Zug, Besten Zug zeigen und Selbst verbessern ruhig ein bisschen höher, sodass für die Zug-Histroy mehr Platz ist
  * Reservier für den Zur aktuellen Stellung Knopf einen eigenen Platz unter der Zug-History, sodass er nicht mehr über der Zug-History aufploppt und für den geplanten Doppelklick Probleme machen könnte.
* Branchmodus:

  * Füge in den Einstellungen die Möglichkeit, dass eine neue verbesserte Branchknoten-Positionierung erfolgt (als Beta-Test -> also die alte Positionierung behältst du noch standardmäßig bei, aber füge das einfach als optionale Einstellung hinzu, damit ich kontrollieren kann, ob das funktioniert):

    * Fixe, dass in den oberen und unteren Rändern die Knoten immer weiter nach außen positioniert werden, obwohl eine rein horizontale Positionierung des nachfolgenden Knoten möglich wäre (das passiert v.a. an den Rändern, deswegen sage ich das so)
    * Reduziere die Anzahl von unnötigen Kreuzungen von Knotenverbindungslinien
    * Vom Übergang von Gegnerzug zu eigener bester Zug sollte die Verbindungslinie, da wo möglich komplett horizontal sein
    * Wenn aus einem eigenen Zug mehrere potentielle Gegnerzüge entstehen (und sich der Branch entsprechend aufspaltet) positioniere den eigenen Knoten vertikal so zentral wie möglich zu den nächsten Gegnerknoten (vorausgesetzt kein andere Knoten blockiert dich)
    * Behalte dabei bei, dass die Knoten mit dem selben Vaterknoten so nah wie möglich beieinander chillen.
    * Behalte sonst die Prinzipien der alten Positionierung (bzw. die Zentralität)"
* 11.10. 11:48: "Ändere folgendes:
* Allgemein:

  * Erhöhe die Kohärenz der verschiedenen Modi untereinander, indem du die Animationsfunktion der Zughistory in allen anderen Modi integrierst, bei welchen es eine Zughistory gibt (Bewertungen o.ä. zusätzliche Funktionen von Eigene Spiele \& Analyse sind natürlich nicht vorgesehen)
  * Falls in den einstellungen bei einem Tab der Titel zu groß ist, füge die Funktion hinzu, dass man über das Hovern, den Vollständigen Titel lesen kann.
  * Mach es Möglich die Größe des Einstellungsfensters anzupassen.
* COG:

  * Die Ladezeit für die Generierung einer weiteren Reihe der Inputeingabe für die Tiefenabschnitte dauert immernoch zu lange. (damit meine ich, wenn man Parameter Anzahl Tiefenabschnitte erhöht, dass ja eine neue Reihe für die untergeordneten Parameter erstellt wird, um diesen Abschnitt zu customizen, es dauert aber \~2s bis diese zusätzliche Reihe erstellt wird und währenddessen Tauchen und bewegen sich diese Pfeile, welche bei jedem Parameter vorhanden sind, und es sind während dieser Ladezeit mehr Vorhanden, als am Ende benutzt werden, sieht ganz komisch aus)
  * Entferne den Knopf Exakt Schätzen (Langsam) -> entsprechend sollte Kartenzahl neu zählen die selbe Breite bekommen, wie seine unteren Knöpfe.
* Branchmodus:

  * Implementiere die Beta-Positionierung als Standard, setze die alte in den Einstellungen als Legacy-Positionierung ein (Genau an der stelle, wo die Beta sich gerade befindet)
  * Füge in den Einstellungen (also wieder diese linke Spalte) die Möglichkeit hinzu die Breite des Graphens visuell zu erhöhen (also den Abschand zwischen den Knotenreihen zu erhöhen) -> Es soll bis zu 5x möglich sein
* Eigene Spiele \& Analyse:

  * Der Knopf "Zur aktuellen Stellung" taucht gar nicht mehr auf.
  * Beim Drücken von vorheriger Zug, wird ja der zukünftige Zug rückwärts abgespielt. Implementiere das auch, wenn man über die Zughistory die Linke Pfeiltaste drückt -> selbiger Mechanismus, soll auch in allen anderen Modi vorkommen, in welchen es eine Zughistory gibt.
  * Implementiere auch hier die Sounds (für das Spielen der Figuren -> für jede gespielte Animation einen eigenen Figurenbewegsound (also, wenn man bspw. durch die Highlights geht (o.a. über die Zug-History), dann sollte pro Zug ein eigener Figurenbewegsound abgespielt werden), und den Bestätigungs und Ablehnungssound um anzuzeigen ob der Zug gut war oder nicht -> da nur, wenn man auch das entsprechende Icon sieht auf dem Board)
  * Beim Spielen der verschiedenen Figurenanimationen passe das leicht an: Bei langen Zugfolgen, sollten die einzelnen Figurenanimationen immer schneller werden. Vor dem letzten Zug, sollte eine kurze Pause gemacht werden (\~0,2s) und dann die Animation in normaler Dauer abgespielt werden.
* Freies Spiel:

  * Mach es möglich mehrere Könige pro Seite aufstellen zu können. Beim Platzieren mehrerer taucht pro Seite ein Input auf, mit welchen man einstellen kann, ob für den den Sieg alle Könige umgebracht werden müssen, oder nur einer von diesen. Implementiere auch hier die Warnung, dass Stockfish damit nicht arbeiten kann und sie entsprechend ausgeschaltet ist (ähnlich wie bei den Bauern).
* Rätselmodus:

  * Die Modiauswahl, soll im genau selben Stil sein, wie bei Schachblick (Zentrierung, Schriftgrößen, Knopfgrößen, ...) -> Koheränzerhöhung
* Schachblick:

  * Im Titel eines Modi steht "Angreifer \_Verteidiger" -> sorge dafür, dass da "Angreifer \& Verteidiger" steht
  * Statistik:

    * Erhöhe den Sättigungs/Helligkeitskontrast innerhab jeder Fehlerkathegorie, sodass sie sich leichter voneinander unterscheiden können.
    * Bei Fehler pro Durchgang, sollte es im Liniengraph, ohne eine spezifische Auswahl die Summenlinie auch angezeigt werden können.
  * Füge innerhalb eines Modus die Frage ein, ob man wirklich den Durchgang verlassen möchte. Dabei soll auch das Spiel Pausiert sein
  * Ersetze die Pausenfunktion für die Koheränzerhöhung mit der schon im Lokalen Spiel implementierten."
* 12.10. 1:30: "Ändere folgendes:
* Allgemein:

  * Benutze in allen Schachmodi (v.a. bei Eigene Spiele \& Analysen, Freies Spiel \& Lokales Spiel) für Remis, Matt gesetzt und Matt die entsprechenden Icons, welche ich seit längerem erstellt hatte, auch konsequent. (Bei der Analyse wird Matt gesetzt das Icon richtig benutzt und manchmal auch das Remi Icon (aber bisher für die Figur, welche als letztes vor dem Remi bewegt wurde)). Das Remi Icon sollte immer nur über die Könige auftauchen. Da wo alle von dir generierte Icons benutzt werden, sollen durch meine Ersetzt werden.
* Schachblick:

  * Statistik:

    * Es sollte überall dort, wo es eine Aufteilung in mehrere Fehlertypen gibt eine Summenlinie angezeigt werden können. Also neben Fehler pro Durchgang auch bei Fehlerzusammensetzung und gesamte Fehler.
    * Ändere die Reihenfolge der Diagramme im Dropdownmenü zu: Durchgänge, Geschwindigkeit, Gesamte Fehler, Fehler pro Durchgang \& Fehlerzusammensetzung
    * Füge bei den Fehlerdiagrammen hinzu, dass die Balkenverbindung an Tagen wo nicht gespielt wurde überspringt und die Verbindung entsprechend über den leeren Tagen hinweg geht.
    * Passe die Farben der Fehlertypen so an, dass es über die Submodi verteilt aus dem gleichen Set von Farben zugegriffen wird. Bisher ist es ja so, dass jede Fehlerkathegorie (bspw. Läufer, Dame, ...) eine Grundfarbe haben und es dann pro Modus bis zu 3 Abweichungen dieser Farbe gibt je nach Sättigung und Helligkeit. Übernehme deswegen die Farben von dem Modus "Angriffe im nächsten Zug" (-> 2 Farben pro Figur) und jene von Zugvergleich (-> mit den vorherigen 2 insg. 3 Farben pro Figur), wobei du hier die Sättigung ein wenig erhöhen darfst, sodass noch die alte Farbe erkennbar bleibt.
  * Figuren merken:

    * Der Bestätigenknopf fehlt (wie bei den anderen Modi, entweder durch mausklick oder mit Leertaste bestätigen). -> bzw. entferne den Aufbau bestätigen Knopf auf der rechten Spalte und füge ihn da ein, wo ihn alle erwarten, über den Pauseknopf
    * Entferne folgende Einstellungsoptionen auf der rechten Spalte: Brett drehen, Weiß/Schwarz am Zug, Rochaderechte, Fehlermeldung durch Stockfish
  * Die Submodi Figur retten, Zugvergleich \& Figuren merken sollten nicht nach Start über das Hauptmenü ein neues Fenster erstellen, sondern wie die anderen 3 vorherigen Submodi im selben Fenster abgespielt werden, in welchen auch das Hauptmenü war (entsprechend ändert bei den noch nicht richtig implementierten Submodi auch die Funktion des Zurückknopfes, indem nicht mehr das Fenster geschlossen wird, sondern indem man tatsächlich zurück zum Hauptmenü gelangt)
* Lokales Spiel:

  * Bei der Eingabe der Namen (also über die Input-Textfelder), sollte der Fokus auf das Textfeld entfernt werden, wenn man mit der linken Maustaste daneben klickt (bzw. bestätigt, wenn dort etwas geschrieben wurde), oder mit Enter bestätigt (bisher ändert sich nur der Fokus, wenn ich ein anderes Inputfeld anklicke)
  * Der Parameter "Deine Seite (für die Partieanalyse)" ist doch eigentlich irrelevant, weil das 1. nicht in der PGN eingetragen wird und 2. die Analyse eh für beide Seiten getätigt wird. Falls der Parameter tatsächlich keine Funktion hat, einfach entfernen.
  * Nachdem man auf Spiel Analysieren geklickt hat, sollte sich eigentlich das Fenster "Eigene Spiele und Analysen" sich öffnen, und die Funktion des Partie analysieren Knopfes ausgeführt werden.
  * Bei Eigene Spiele \& Analyse
* Eigene Spiele \& Analyse:

  * Bei Highlights durchgehen spiele den Bestätigungs-/Ablehnungsound nur ab, wenn der Highlightzug gespielt wurde bisher ist es nämlich so, dass die beiden Sounds nach jedem Zug abgespielt werden, auch wenn sie nicht als Highlightzüge gewertet wurden. (Entsprechend sollte auch während der Analyse beim Wechsel zur nächsten Highlightstellung kein Analyse-Icon angzeigt werden während der Animation, wenn es sich nicht um einen Highlightzug handelt und man auf "Nächster Highlightzug" getippt hat)
  * Nächster Highlightzug/ Highlights durchgehen sollte keine Funktion haben, wenn das Spiel noch nicht analysiert wurde. Ersetze stattdessen den Highlights durchgehen Knopf (in selber Größe, Gestallt \& Positionierung) durch einen Fehler analysieren Knopf, welcher auch bestehen bleibt, wenn man nicht mehr in der Startstellung sich befindet.
  * Der Knopf Zur aktuellen Stellung taucht immernoch nicht auf unter der Zughistory.
* Freies Spiel:

  * Bei Mehrkönigspiel sollte auch ein Feedback kommen, wenn das Spiel zu Ende ist, das geschieht durch Sound und durch das Matt gesetzt Icon über die Figur, welche den letzten König geschlagen hat.
  * Wenn man auf den Zurückknopf drückt, dann sollte die Stellung bearbeitet werden können, welche im Board gerade angezeigt wird und nicht, welche die eigentliche aktuelle Stellung ist.
* Rätselmodus:

  * Taktikmodus:

    * Lebensmodus:

      * Setze die maximale Anzahl von Leben auf 7.
      * Wenn es Leben gibt, dann zeige die Leben über echte Herzicons an, welche nach einem Fehler nacheinander aufgebraucht werden.
      * Entferne die Knöpfe (nur im Lebensmodus) Nächstes Rätsel und Gesamte Lösung zeigen, wobei der Lösung zeigen Knopf eigentlich die Funktionalität vom Gesamte Lösung zeigen Knopf annehmen sollte. Beim betätigen dieses Knopfes sollte ein Leben abgezogen werden.
      * Füge den Knopf Neuer Durchgang ein mitten im Bildschirm, wenn Modus beendet, samt Info zum aktuellem Score und Highscore.
    * Zeitmodus:

      * Beim Falschen Zug, sollte der Score -1 gerechnet werden (0 ist aber das Minimum)
      * Stelle die übrige Zeit wie bei der Schachuhr im Lokalen Modus dar. (entsprechend zwei seperate Uhren für pro Zug und insgesamt, wenn beide vorhanden).
      * Implementiere beim pro Zug Modus, wenn es noch zusätzlich eine Gesamtzeit gibt, ob beim Ablauf dieser Uhr nur ein Minuspunkt vergeben werden sollte, oder das Ganze abgebrochen.
      * Entferne den Nächstes Rätsel Knopf, wenn pro Zug aktiv, wenn nur insgesamte Zeit, dann -1 Score.
      * Füge den Knopf Neuer Durchgang ein, wie beim Lebensmodus.
    * Entferne den Preset löschen \& Alte Einträge wiederherstellen Knopf auf der linken Spalte. Mache die restlichen unteren Knöpfe von der Breite her gleich groß, fast die gesamte Breite der linke Spalte einnehmend.
    * Reduziere den vertikalen Abstand zwischen den Parametern Rätselschwierigkeit, Zeit pro Zug, Zeit insgesamt, Leben und Bei Fehler Lösungszug anzeigen. Erhöhe den Abstand leicht zwischen Name des Presets \& Rätsel-Schwierigkeit und zwischen Bei Fehler Lösungszug anzeigen \& den unteren Knöpfen. Reduziere den Abstand zwischen Mittelspiel: mindestens gute Züge bzw. höchste gute Züge
* Lernmodus:

  * Entferne den Lautstärke und den Einstellungsknopf.
  * Ersetze die Engineeinstellung von Start bzw. Schwach zu derselben wie beim Freien Spiel -> Suchtiefe \& Intelligenz.
  * Lösche die Anzeige mit "Weiß/Schwarz am Zug" -> sollte nämlich beim Lernen klar sein und Eckenanzeige genügt. Genauso die Anzeige "Hintergrundanalyse . +Weiß / - Schwarz".
  * Erhöhe entsprechend leicht den Platz für die Zughistory nach oben.
  * Bearbeite den < Openings Knopf zum einfachen Back-Knopf wie beim Schachblick oder beim Rätsel (Funktion wird beibehalten, nur Visuelles wird angepasst) -> Kohärenzerhöhung.
* Einstellungen:

  * Beim Hovern über einen Tab, sollte der ganze Name angezeigt werden.
  * Der Inputkasten für die Schwirigkeitseinstellung bei Figuren Merken beim Schachblick-Tab ist unten leicht abgeschnitten
  * Sortiere die Tabs von links nach rechts neu: Allgemein, Stellungsbewertung, Markierungen, Partieanalyse, Branches, Schachblick.
  * Füge links neben jeden bearbeitetem Parameter über so ein Neu Laden Piktogramm, die Möglichkeit ein die Einstellung des Parameters auf den Standard neu zu setzten."
* 12.10. 7:10: "Arbeite daran weiter, was du nicht beenden konntest.
Achte aber dabei, dass die auf Standardeinstellung zurücksetzen Buttons erstens nur auftauchen sollen, wenn sie von der Einstellung abweicht und die im selben Stil, wie die neuladen Buttons von den Enginevarianten sind (auch von der größe genauso, und KEIN Kasten drumherum)"

12.10. 12:19: "Ändere folgendes:

* Allgemein:

  * Einstellungen

    * Beim Hovern über den Tabs, taucht immernoch nicht der ganze Name auf obwohl durch die kleine Fenstergröße die Namen entsprechend nicht vollständig angezeigt werden können
    * Bei den Schachblick Einstellungen wenn man zum ersten mal nach dem öffnen zu diesem Tab wechselt, taucht mittig über die Figuren merken Eingabe ein weißer Pfeil auf, der verschwindet, wenn ich das Fenster der Einstellungen resize
    * Ändere auch bei Slate \_Ivory (Fix auch den Namen, wahrscheinlich müsste da ein \& hin) Stil auch das aussehen der Figuren nicht nur die Farben
    * Benenne das Farbschema V10 Original um in COT Standard - Dark
    * Füge bei Schachblick auch Die Einstellung für maximale parallele Rätsel-Engine \& Threads je Rätsel-Engine hinzu
  * White-Mode (belasse die Farben bei COT Standard - Dark gleich, nur in den folgenden Sachen hast du wahrscheinlich vergessen, dass man das auch einstellbar machen sollte in UI-Farbschemata):

    * Branch-Modus die Knotenfarben sind zu dunkel (v.a. das ungelernte)
    * Der Hintergrund von manchen Knöpfen bspw. Zug zurück oder neue Partie bei freies Spiel oder Training starten bei Rätsel-Einstellungen, ist zu hell, damit man die weiße Schrift noch gut lesen kann. -> Mach die Schrift Schwarz, belasse die eigentliche Knopffarbe
* Lernmodus:

  * Ändere die Knöpfe: Nächste Karte, Weiterspielen und Branch anzeigen so, dass sie im selben Stil sind wie bspw. bei Eigene Spiele und Analyse -> also durch den Stil veränderbar, beim V10-Stil in Schwarz (kein blau) -> Kohärenzverbesserung (beim Freeplay genauso)
  * Mach es möglich, nachdem man eine Lernkarte richtig gespielt hat, dass man selber die Gegnerische Figur weiterbewegen kann und man selber gucken kann, was aus verschiedenen gespielten Zügen folgt (also nicht zwarngsläufig nur auf Weiterspielen drücken müssen)
* Branchmodus:

  * Füge als Beta (über linke Spalte aktivierbar), übergeordnet noch einen übergeordneten Kontrollör hinzu, welcher sich anschaut, wo genau noch eine Kreuzung von Knoten besteht, und ob man diese durch minimale Verschiebung durch einzelne Knoten beheben kann (bisher gibt es nämlich noch bspw. beim Branch "e4 d6 d4 Nf6 Nc3 g6 f4 Bg7 Nf3" noch ein paar vermeidbare -> falls du dir nicht sicher bist, ob der wirklich immer ein besseres Resultat liefert, dann füge noch hinzu, dass die Umpositionierung der Knoten nur applyed wird, wenn es insgesamt weniger Knotenverbindungskreuzungen gibt)
  * Füge in der obersten Zeile noch einen Knopf hinzu (bzw. einen Menüpunkt), welcher sich die eigenen Spiele anguckt, um zu schauen, wie oft Spiele gespielt wurden, wo solch eine Endposition erreicht wurde:

    * Dazu werden die Endstände mit einer Umrandung markiert (also immer um den Knoten, welcher die letzte gemeinsame Stellung von diesem Match beinhaltet) -> auch standardspieler auswahl notwendig, wenn der Modus aktiv
    * Stellungen, welche auf Grund des Gegners nicht außerhalb der Startstellung gelangen (also damit meine ich die ersten Züge, welche keine Varianten haben im Branch), werden nicht markiert (wenn man durch eigenes verschulden außerhalb des Branches gelangt ist, oder wenn dies der Gegner getan hat, aber während sich der Branch schon in der Phase befindet, dass es mehrere Spielmöglichkeiten für den Gegner befindet)
    * Zur Info mit dieser variablen Phase, will ich einfach nur diese Anfangsphase ausschließen, welche immer gleich ist
    * Während man diesen Modus ausgewählt hat und einen markierten Knoten ausgewählt hat erscheint zusätzlich in der rechten Spalte eine Liste von den Spielen, welche diese Position erreicht haben. Nach Auswahl eines der Spiele, öffnet sich Eigene Spiele und Analyse mit genau dem ausgewählten Spiel, mit dem letzten Theoriezug
    * Nur falls ein Knoten mehrmals bespielt wurde, dann gib deren Menge oben rechts außerhalb des Knoten aus (also die Zahl soll maximal dieselbe Höhe haben wie die obere Kante des entsprechenden Knotens, aber horizontal darf er weiter abstehen, ähnlich große Schriftgröße wie die Züge innerhalb der Knoten nur in dick)
  * Füge noch einen Bearbeitungsmodus hinzu:

    * Hier taucht in der rechten Spalte die Möglichkeit Knotenvarianten Hinzuzufügen (am schon bestehenden Schachbrett kann man einfach den nächsten Zug spielen) und mit einem Bestätigen Knopf einfügen -> dabei ist das System so schlau, dass wenn es diesen Knoten schon gibt die Auswahl direkt zum entsprechenden Knoten springt und es gibt kein Duplikat
    * Bei der Erstellung von Knoten sind auch mehrere Zugabfolgen im Brett möglich
    * Dann gibt es auch noch automatisch die Möglichkeit über einen Knopf den nächsten besten Zug zu generieren (Enginetiefe einstellbar) -> nur Board ändert sich, Knotengenerierung muss immernoch mit dem originalknopf bestätigt werden ("Spiele den besten Zug")
    * Es gibt die Möglichkeit direkt einen Knoten zu generieren mit den verschiedenen möglichen Varianten (Leichtigkeitsgrenze \& maximaler gegnerischer Verlust einstellbar)
    * während der Erstellung wird eine simple Branchpositionierung benutzt die normale wird nach dem bestätigen der Bearbeitung benutzt -> damit es nicht die ganze Zeit bei der Erstellung lagt
    * füge einen Knoten löschen Knopf hinzu bzw. beim auswahl einer Variante Variante Löschen (Knopf ändert sich entsprechend der Auswahl)
    * Tiefer erforschen \& mit anderen Opening kombinieren Knopf verbleibt
  * Für die oben genannten Submodi soll es auf der oberen Zeile ein Drop-Down-Menü geben mit folgenden Namen: "Lernmodus" (Standard), "Bearbeitungsmodus", "Analysemodus"
* COG:

  * Setze den Abstand zwischen Anzahl Tiefenabschnitte und Lernperspektive so wie zwischen Name und Lernperspektive, denn zwischen Lernperspektive-Inputeingabe und Anzahl Tiefenabschnitte ist ein bisschen zu wenig Platz -> zwischen Anzahl Tiefenabschnitte und Parameter für Tiefenabschnitte darf der Abstand entsprechend (im gleichen Maße) kleiner sein
  * Wenn ein Tiefenabschnitt komplett vor dem Beginn der Komplexitätsprüfung liegt, dann soll die Inputeingabe für diesen Abschnitt für Komplexitätsgrenze, Komplexitätsreserve (KGR): Topzüge und KGR-Bewertungsgrenze (Weißsicht) entfernt werden -> haben ja dann für diesen Abschnitt keine Relevanz mehr
  * Die Abschnittstiefengrenze von einem folgenden Abschnitt darf nicht kleiner oder gleich dem vorherigen Abschnitt sein
  * Wichtig: Du hast immernoch nicht nicht den Bug entfernt, dass während dem umstellen der Tiefenabschnitt-Anzahl noch ein paar Pfeile der Inputkasten sich visuell nach links und rechts bewegen und es immernoch eine gewisse Zeit dauert, bis tatsächlich die Kästen erscheinen (ich meine aber, dass du es leicht schneller gemacht hast, aber eigentlich sollten sich die Inputkästenanzahl direkt richtig updaten) -> das zappeln dieser zusätzlichen Pfeile ist immernoch da
  * Im Fullscreen, wenn man langsam die Tiefenabschnittszahl verändert und von 4 -> 3 oder tiefer wechselt (egal ob mit Pfeiltasten oder über schreiben) sieht man am rechten Rand noch einen Teil der vorherigen Inputkästen
  * Wenn ich in einem Inputkasten manuell etwas eintrage (über Tastatur) und dann über Enter bestätige, dann kommt eine Fehlermeldung (als Sound) stattdessen sollte es keinen Fehlersound geben und man sollte automatisch den Fokus zum darunterliegenden Inputkasten wechseln:

    * Durch die obere und untere Pfeiltaste (links und rechts, sollten weiterhin für die Bearbeitung von dem geschriebenen Text benutzt werden können) sollten nicht mehr die Größe der Inputs geändert werden können, aber auch der Fokus
    * Bei den Tiefenabschnitten sollte sich erst von oben nach unten und dann von links nach rechts der Fokus verändert werden (also zwischen vorherigem KGR-Bewertungsgrenze (Weißsicht) und nächsten Abschnittstiefengrenze ist der Übergang des Fokuses über Pfeiltasten bzw. Enter -> erst nach letztem KGR-Bewertungsgrenze Weißsicht wird zu Komplexitätsprüfung ab Tiefe (Halbzüge) gesprungen)
  * Bei wachsender Opening Branch ist dieser Ladebalken erstens nicht vollständig im Fenster (wird an dern unteren Ecken abgeschnitten) \& zweitens passt er nicht zum Stil der App -> ändere entsprechend seine Farbe (Funktion ist prima)
* Rätselmodus:

  * Bei Schnelles Spiel sollte nicht noch zwischendrinn die Rätseleinstellungen kurz aufflackern, sondern man sollte direkt rein
  * Rätseleinstellungen:

    * Wenn ich auch hier mit Enter bestätige Inputkästen, dann sollte auch hier kein Fehlerton vorkommen aber hier sollte nur der Fokus abgebrochen werden (kein switchen des Fokus über Pfeiltasten oder Enter) -> beim kompletten Löschen der Zahl, sollte sich der Wert auf seinen Standardwert setzen (bspw. 0)
    * Unter Training starten ist ein leerer Knopf/Kasten zu sehen (wenn keine Funktion, dann entfernen)
  * Taktik:

    * erstell eine Animation, dass man ein Herz verloren hat (probier mal zwei aus über Einstellung einstellbar -> Herz wird schneller immer größer und durchsichtiger; Herz zerbricht in 2 Teile seine Bruchstücke fallen werden leicht größer und schnell durchsichtiger)
    * Wenn "Keine Leben mehr." auftaucht füge einen weiteren Knopf ein "Zurück zum Menü" (Rätselmodi Menü öffnet sich) \& bei einem Neuen Highscore sollte statt "Keine Leben mehr.", "Neuer Highscore!" auftauchen. -> ähnlich auch bei den Zeitmodi
    * Wenn man auf Nächstes Rätsel klickt im Zeitmodus, sollte man auch einen Fehlersound bekommen bei Nächstes Rätsel, Minuspunkte sollten jetzt doch auch möglich sein, wenn man mehr Fehler + Nächstes Rätsel als Erfolge macht (Also Fehler und Nächstes Rätsel konnen beide den Score nach unten ziehen) -> der Highscore verbleibt auch beim ersten mal spielen bei 1
    * Wenn es nur Zugzeit als Requirement gibt, dann sollte bei Ablauf der Zugzeit kein Punkt abgezogen werden können (bisher ist das noch möglich, wahrscheinlich, weil du nur die Option, dass Zugzeit -1 Punkte gibt hidest, aber nicht es wirklich deaktivierst)
    * Bei Kombi aus Zugzeit und Gesamtzeit, wenn das Spiel durch Zugzeit abgebrochen wird, dann erscheint keine Soundfehlermeldung
* Eigene Spiele \& Analyse:

  * Entferne auf der oberen Zeile den Partie analysieren Knopf
  * Bei der Analyse der Partie von einem Lokalem Spiel wird unten links nicht der Fortschritt angezeigt
* Lokales Spiel:

  * Füge bei den Einstellungen eine Option hinzu, das während dem Spiel fortlaufend die Analyse erstellt wird (damit man nicht auf die Analyse so lange warten muss)
* Schachblick:

  * Statistik:

    * Mach die Farbreihenfolge global wie bei Figuren merken, dass zuerst die hellere Farbe und dann die dunklere Farbe in der Legende zu sehen ist -> Verändere dabei in keiner Kathegorie die Farben die du benutzt hast (außer bei Zugvergleich hier hast du die blassen Farben benutzt, solltest aber die saftigen benutzen) oder die Reihenfolge in welcher du die Fehlertypen aufgeschrieben hast mir geht es nur um die Reihenfolge der benutzten Farben (bspw. hast du bei Angreifer \& Verteidiger zuerst die dunklere und dann die saftigere Farbe benutzt und bei Dame saftig, dunkel, blass, obwohl überall sonst saftig vor dunkel kommt (bei Dame speziell muss es blass, saftig, dunkel sein))
    * Mach die Balkenverbindungen ein bisschen ähnlicher zur Originalfarbe
    * Bei Durchgänge \& Gesamte Fehler, sollten die Zahlen im Diagramm auf der vertikalen Achse int sein
  * Von wechseln von Fullscreen zu Windowed wird der Pauseknopf nicht resized
  * Wenn Pausiert, soll über die Leertaste fortgefahren werden können (auch ohne, dass direkt zusätzlich bestätigt wird)
  * Mach die Regelerklärung für die Submodi deutlich kürzer, du musst nicht erklären wie die Steuerung funktioniert, wie die Engine das macht oder dass es keine mathematische Garantie gibt, nur das was für den ersten Benutzer am relevantesten ist
  * Bei Gegnerische Angriffe setze die Markierungen, welche gefehlt haben auf rot
  * Bei Figur retten verbleiben die anderen Markierungen welche auch möglich gewesen wären (halt rotiert, weil sich das Board auch rotiert)
  * Mach bei Zugvergleich so, dass unter den Boards horizontal die Evaluationsanzeige auftaucht nach der Auswahl
  * Wenn (bspw. bei Zugvergleich) vorher der Durchgang abgebrochen wird, dann müssen jene Positionen nicht verworfen werden, welche noch nicht gesehen wurden
Beende deine Arbeit erst, wenn du 100% der Änderungen übernommen hast."
* 14.10. 18:02: "Arbeite daran weiter, was du nicht beenden konntest"
* 16.10. 11:57: "Ändere folgendes:
* Allgemein:

  * Unter alle Inputkasten, welche ein Dropdownmenü haben flackert der Dropdownpfeil weiß bei Bewegung des Mauszeigers (Das gilt für die Einstellungen, Rätsel-Einstellungen, COG, ...).
  * Bearbeite die Figuren von Slate \& Ivory:

    * Die Spitzen der Krone der Dame und die Köpfe der Bauern bestehen ja aus Kreisen. In den Kreisen sieht man die Linien der anderen Figurenbestandteile. Entferne diese Linien, sodass die Kreise praktisch im Vordergrund erscheinen.
    * Diese Linien sieht man auch am Grund (der untere Bestandteil) der Figuren.
    * Beim Kreuz des Königs hingegen sollen seine Linienbestandteile, welche in den restlichen Figurenkörper hineingehen gelöscht werden. Das Kreuz gelangt in den Hintergrund.
  * Mach im Whitemode die Lila Schriftfarbe im Branchmodus dunkler.
  * Bei der UI-Farbschema Einstellung lösche COT aus COT Standard - Dark
* COG:

  * Füge in den Tiefenabschnitte eine neue Einstellung (mit Erklärung über hovern) hinzu, mit welcher man einen Evaluations Cutoff für die zufällige Variation seiner zu spielenden Antworten einstellen kann (float, 0.01 Schritte, Standard 0.00 = deaktiviert, Lernkompression hat höhere Priorität). Zur Erklärung, bisher wird für den eigenen zu lernenden Zug (Antwort) immer der beste Zug gespielt, aber mit dieser Einstellung wird aus einem Set von Zügen (bestehend aus allen Zügen mit einer Evaluation > beste Evaluation - eingestellter Wert) ein zufälliger Zug ausgewählt. (-> Individualisierung seines Branches)
  * Wichtig: Du hast für die Tiefenabschnittzahl 1-3 die Ladedauer und das visuelle Bewegen von Inputpfeilen gefixed. Für 4-5 gilt das noch nicht.
  * Wenn man mit der Maus danebenklickt, soll der Fokus aufgehoben werden.
* Lernmodus:

  * Ändere dem "Zur aktuellen Stellung" Button zu dem selben Stil der anderen Buttons. Das gilt für Freeplay genauso.
  * Wenn die Lernkarte noch nicht gelernt wurde zeige auch nicht die Enginevarianten an, wenn man in der Zughistory zurückgeht.
  * Wenn man weiterspielt markiere in der Zughistory den gelernten Zug der Lernkarte. Füge auch noch, wenn man weiterspielt, einen zusätzlichen Platz unter der Zughistory hinzu mit welcher man zurück zur Lernstellung hinzu.
  * Kombiniere den Lösung zeigen \& Nächste Karte Button zu eins (wenn noch nicht Lösung gezeigt, dann Lösung zeigen, wenn Lösung schon gezeigt, dann Knopf in Nächste Karte umbenennen). Verschiebe diesen Button also horizontal langen Button unter dem Spielbrett + Evaluationsanzeige.
  * Wenn man auf "In Branch anzeigen" klickt (darin umbenennen), wird bisher, wenn genau die Zugfolge nicht existiert, die Startposition im Branch angezeigt. Ich möchte aber, dass dann geguckt wird, ob es den Branch mit der Zugfolge ohne den letzten Zug existiert, und dann ob jene ohne letzten und vorletzten Zug, ... . Das gilt für Freeplay genauso.
* Freeplay:

  * Der Ab hier weiterspielen Button, soll nicht einen neues Tab öffnen, sondern die Zughistory auf den ausgewählten Zug zurücksetzten.
* Branchmodus:

  * In der rechten Spalte befindet sich unter dem Board Debug-Info mit "Knotenerstellung - Hintergrundanalyse ...". Mache diese nur sichtbar unter Debug-Info, sonst verringere den Abstand zwischen Einstellung und Board deutlich.
  * Der Kreuzungen vorsichtig reduzieren (beta) Modus funktioniert nicht. Teste die Funktion anhand von "e4 d6 d4 Nf6 Nc3 g6 f4 Bg7 Nf3" und versuche so lange bis der Modus real Kreuzungen reduziert.
  * Wenn die Auswahl aufgehoben wird (also ein Knoten nicht mehr ausgewählt ist), sollte die gesamte rechte Spalte verschwinden. (Bei erneuter Auswahl eines Knoten soll die rechte Spalte natürlich wieder erscheinen.)
  * Analysemodus:

    * Entferne den Ausgewählte Partie Analysieren Knopf -> wird durch das simple draufklicken ersetzt (welches bereits schon existiert)
    * Ich glaube die Zuweisung Eigene Spiele -> Knoten funktioniert nicht richtig. Es wurden 4 Zuordnungen von all meinen upgeloadeten Matches gefunden (an den richtigen Stellen, 3 nach e4, einer am Ende kurz bevor es variabel wird), während ich per Hand einen gefunden habe, welcher nicht in Branch repräsentiert ist, obwohl er es sein sollte.
    * Füge einen Knopf hinzu, welcher alle Farben der Subbranches ausblendet (die subbranches bleiben aufgeklappt).
    * Mach die Farbe der Markierungen für benutzte Knoten standardmäßig rot, aber veränderbar an der selben Stelle, wie im Lernmodus für die Subbranchfarbe.
  * Bearbeitungsmodus:

    * Für die rechte Spalte gibt es 2 Slider. Komprimiere sie zu einem Slider, sodass nur noch der ganz rechte, äußere vorhanden ist. (Zusätzlich gibt es auch unten in der rechten Spalte einen dritten horizontalen Slider -> der kommt weg)
    * Füge für die Engine-Einstellungen unter einem separatem aufklappbaren Tab innerhalb der rechten Spalte.
    * Dieser weiß umrandete Kasten für die Liste der plausiblen Gegnerantworten, sollte nur auftauchen, wenn auch die Liste einen Inhalt hat.
    * Füge auf der selben Zeile von Entwurf zurücksetzen, um einen Zug zurück zu gehen.
    * Die Reihenfolge der Inputs sollen so sein (umbenennen, zu den folgenden, wenn so) (also keine Knöpfe mehr dunkler machen, wenn nicht benutzt, sondern die tatsächliche Menge an Knöpfen ändern, weil wir haben hier nicht so viel Platz) (nicht benannte Knöpfe werden entfernt):

      * "Stellungsvarianten" Dropdown
      * "Engine Einstellungen" Tab
      * "Bester Enginezug spielen"
      * "Gegenantworten vorbereiten"
      * optionaler Listenkasten (wenn Gegenantwort vorbereitet)
      * Zug zurück als "<" + "Zugfolge zurücksetzen" (nur anzeigen, wenn Zugfolge vorhanden)
      * "Knoten hinzufügen" (nur anzeigen, wenn Zugfolge vorhanden) bzw. "Knoten löschen" (nur anzeigen, wenn kein Entwurf für Zugfolge) bzw. "Stellungsvariante löschen"
      * optional kleiner Abstand (wenn dadrunter Knopf vorhanden)
      * "Tiefer erforschen" (wenn keine Zugfolge)
    * rechte Spalte wird auch ausgeblendet, wenn kein Knoten ausgewählt
    * in einer Zeile "Bearbeitung bestätigen" und "Bearbeitung abbrechen" Knopf unten nur in der mittleren Spalte, wo der Branch angezeigt wird -> nur wenn der Branchmodus-Fenster bspw. vor Bestätigung gelöscht wird, wird es noch nicht applyed (wenn aber möglich, dann speichere den Zwischenstand vom Bearbeitungsmodus)
* Eigene Spiele \& Analyse:

  * Die Fehleranalyse sollte stärker im Hintergrund passieren können, sodass man auch währenddessen andere Highlights von Games durchgehen kann oder andere Matches in die Warteschlange der Analyse hinzufügen kann. Weiterhin soll man PGN schreiben, Partien entfernen können, ... .
  * Während der Analyse, verändere den Fehler analysieren Knopf zu "Anaylse abbrechen" bzw. "Aus Warteschlange entfernen" mit den Infos dadrunter.
* Rätselmodus:

  * Die Rätsel-Einstellungen brauchen zu lange, um aufzuploppen, das sollte auch alles auf einmal erscheinen.
  * Füge im entsprechenden Kasten für Zugzeit abgelaufen, Keine Leben mehr., ... oben rechts ein X hinzu, sodass man die Lösung des Boardes sich nochmal anschauen kann. -> unter dem Brett wird dann Platz gemacht und es erscheint der Knopf "Neuer Durchgang"
  * Wenn ich in den Einstellungen mit Fokus auf Zahleneinstellungen danebenklicke, dann soll der Fokus weg und der Wert soll auf 0 zurückgesetzt werden.
* Lokales Spiel:

  * Ersetze das Bild vom Uhr pausieren Knopf, durch zwei Pause und Play Piktogramme.
* Schachblick:

  * Füge für jeden Modus oben rechts einen aufsteigenden Timer hinzu, welcher die Gesamtzeit pro Durchgang misst (aber bei Pause, auch pausiert), bei Figuren merken, wenn die 10sek runtergezählt werden, soll der aufsteigende Timer vom 10sek absenkenden Timer ersetzt wird, aber während dem setzten den Gesamttimer (zählt nur hoch, wenn der andere Timer nicht runterzählt)
  * Zugvergleich:

    * Füge über Pfeiltasten (links/rechts) die Funktion hinzu zwischen dem vorherigem und den beiden aktuell zu bewertenden Zug zu switchen. Füge entsprechende Knöpfe zusätzlich oben rechts über das rechte Board hinzu.
    * Ersetze den Pause-Knopf zu dem selben, wie bei Lokales Spiel und verschiebe ihn oben rechts. (rechts mit ein bisschen Abstand über die Zugknöpfe)
    * Mach den Knopf "Nächste Aufgabe - Leertaste" so große wie gerade Pauseknopf (denn du ja eh verschiebst) + Nächste Aufgabe Knopf und blende ihn tatsächlich auch komplett aus ohne diese weiße Umrandung, wenn noch kein Zug ausgewählt wurde
    * Die Evaluationsanzeige wird halb mittig von einem grauen Kasten verdeckt, damit die Evaluationszahl auch tatsächlich zu sehen ist. Mach es stattdessen so, dass die Evaluationszahl unter der Anzeige zu schreiben. Verschiebe auch die Anzeige näher an das Spielbrett.
    * Bei Auswahl eines Zuges, färbe die grüne Markierung, welche anzeigt, welcher Zug gespielt wurde beim falschen Zug, nach der Auswahl eines Zuges in rot. Male ein großes dünnes X über das falsche Board.
    * Füge für Playerfeedback die Anzeige hinzu, welches Board man gerade drüberhovert:

      * wenn keines, wird nichts angezeigt, wenn aber schon, dann erscheinen mittig auf jeder Seite des Boardes, um das Board außen herum insgesamt 4 dünne, \~1/4 Seitenlänge lange Linien (entsprechend parallel zur entsprechenden nahen Seite)
      * Bei der finalen Auswahl werden die Linien leicht länger (\~1/3 Seitenlänge) (ähnlich smoothe Animation wie bei der Eckenanzeige), und sie verändert sich nicht mehr mit dem Switchen des Hovern
    * Versuche häufiger Züge zu nehmen, welche näher an +/-0 sind (in Range von -3 bis 3)
    * Nimm keine Züge, bei denen der alleinige Materialgewinn +2 oder mehr ist, und deswegen schon praktisch klar ist, welcher Zug besser oder schlechter ist (max +1, aber am häufigsten +/- 0)
    * Nach der Auswahl eines Zuges, füge unter jedes Board einen Knopf hinzu in welchem steht "In Freies Spiel öffnen".
Beende erst wenn 100% fertig. Achte dabei darauf, dass du (nicht wie im letzten Run) keine zusätzlichen Bugs erzeugst (also verändere nichts, was ich nicht vorher erwähnt habe)."
* 16.10. 17:06: "Arbeite daran weiter, was du nicht beenden konntest"
* 16.10. 21:41: "Ändere folgendes:
* Allgemein:

  * Verringere den Abstand bei den Enginevarianten zwischen unterer Slider und oberen Züge innerhalb derselben Zeile leicht.
  * BUG: Teilweise wird ganz random der gesamte Sound nicht mehr abgespielt, obwohl der Ton auf an gestellt ist. Ein Neustart behebt das Problem wieder.
  * Mach Raum für die Implementation eines Soundes zum Schlagen von Figuren, aber füge keinen entsprechenden Sound hinzu, einfach nichts verändern.
* Lernmodus:

  * Schreibe "- Leertaste" als Suffix von Lösung zeigen \& Nächste Karte
  * Beim drücken auf "Zur Lernstellung" wird nicht die Auswahl der Zughistory geupdated, anders als bei Zur aktuellen Stellung. -> Die Lernstellung, sollte dasselbe sein, wie die markierte Stellung (die Markierung ist richtig, die Stellungsrückstellung wird ist ein Zug zu früh).
  * Halte permanent den Platz für beide Buttons frei unter der Zughistory, damit die Zughistory nicht die ganze Zeit seine Größe verändert. "Zur Lernstellung" wird nur vor der richtigen Auswahl oder danach, aber während man sich auf der aktuellen Stellung befindet, "Zur aktuellen Stellung" nur danach -> diese Buttons werden auf der selben Position angezeigt, stattdessen kommt "Analyse: Zug zurück". Dieser Knopf verändert leicht seine Funktion und wird umbenannt in "Aktuelle Stellung neu setzten" (Soll nur auftauchen, wenn korrekter Zug ausgewählt und ausgewählte Stellung nicht vor der Lernzugstellung, aber vor aktuelle Stellung).
  * Wenn man selber einen Gegnerzug gespielt hat, nachdem man den richtigen Lernzug ausgewählt hat, soll automatisch die Weiterspielfunktion ausgewählt werden mit diesem Zug.
  * Der In Branch anzeigen Button ist jetzt der einzig übrige Button auf seiner Position. Dieser kann permanent angezeigt werden und hat die ähnliche horizontale Breite wie die unteren Knöpfe in derselben Spalte (die Position des Knopfes bleibt vertikal gleich).
  * Der Infokasten kann ein bisschen simpler gehalten werden zu Richtig/Falsch/Lösung-Text + richtiger Zug (wenn richtig oder Lösung), aber ohne Info wie Stockfischtiefe, Wert, Verlust zum besten Zug
* Branchmodus:

  * Implementiere die Beta Kreuzung vorsichtig reduzieren vollständig (das gucken, ob die Verschiebung tatsächlich einen Effekt hat, soll weiter bestehen)
  * Der Horizontale Spaltenabstand, soll in einem Intervall von 0,25 bis 5 einstellbar sein (nicht wie vorher zwischen 1 und 5)
  * Bearbeitungsmodus:

    * Vergrößere den Aufklapppfeil bei dem Engine Einstellung leicht.
    * Wenn ich die Zugfolge zurücksetze (oder so weit auf zurück klicke, bis kein Zug mehr ausgewählt ist), dann soll wieder die Markeirung angezeigt werden, welche anzeigt, welcher Zug gespielt wurde.
    * Neben den Stellungsvariante löschen Knopf füge auch den Knopf "Gesamter Knoten löschen".
  * Analysemodus:

    * Wenn es eine Markierung bei einem Knoten gibt mit mehreren Stellungsvarianten, dann markiere die Züge, welche gespielt wurde in entsprechender Farbe.
* COG:

  * Pro Tiefenabschnitt ist der Abstand zwischen KGR Topzüge und KGR Bewertungsgrenze und zwischen KGR Bewertungsgrenze und Eigene Antwort: Verlustantwort (umbenennen zu maximale Zugvariation) größer als zwischen den anderen Parameter -> so anpassen, dass die beiden Abstände gleich groß sind wie die anderen Parameter (der Abstand der Inputkästen soll wichtiger sein als zwischen den Namen der Parameter).
* Rätselmodus:

  * Beim Drücken auf Schnelle Rätsel aus dem Hauptmenü flackert kurz noch das letzt geöffnete Einstellungs Menü des letzten Submodus auf
  * Beim Drücken auf dem Zurückknopf aus Schnelle Rätsel wird das letzte Einstellungsmenü geöffnet (sollte eigentlich das Rätselmodi Hauptmenü sein)
  * Bei der "Keine Leben mehr."-Anzeige o.ä. taucht die Highscore-Anzeige hinter den unteren beiden Knöpfen
  * Setze den "Zur aktuellen Stelung"-Knopf im selben Stil wie beim Lernmodus -> Kohärenz
  * Mach das Piktogramm für den "Aus Fehlern lernen" Submodus nur noch hell und dunkel (nicht mehr farblich)
* Eigene Spiele \& Analyse:

  * Entferne den Zug zurück Knopf oben links über dem Brett, links vom Brett drehen. Es gibt ja schon den "<" Knopf mit derselben Funktion.
  * Warum existier bei nicht analysierten Zügen, trotzdem teilweise eine Bewertung? Entweder ganz oder gar nicht.
  * Spiele keine Bestätigungs oder Falsch Sounds ab, während innerhalb der Animation zum nächsten Zug über das Anklicken der Zughistory, nur für den ausgewählten Zug (genauso wie es schon passiert bei dem durchgehen der Highlightzüge).
  * Erstelle den Knopf "Evaluationsgraph anzeigen". Dadurch erscheint ein neues Fenster, wo Graphisch die Bewegung der Evaluationsbewertung angezeigt wird über die gespielten Züge (je nach Brettrotation auf der einen Seite ist der Graph schwarz und auf der anderen Seite weiß). Ein strich geht dabei durch den Graphen hindurch, welcher den aktuell betrachteten Graphen anzeigt und mit einem Klick auf den entsprechenden Bereich des Graphen, kann sich die betrachtete Stellung ändern.
* Schachblick:

  * Zugvergleich:

    * Zum bestätigen der anfänglichen Erklärungsanzeige sollte ein Bestätigungsknopf auftauchen -> wie bei Figuren retten \& merken auch (sodass es so ist wie bei den anderen drei nicht erwähnten submodi ist)
    * Mach das X beim falsch so groß, dass es die maße des gesamten Bretts hat.
    * Mach nach Auswahl auch die Dicke der Seitenlinien dicker."
* 16.10. 22:06: "Arbeite daran weiter, was du nicht beenden konntest"
* 17.10. 12:21: "Ändere folgendes:
* Allgemein:

  * Die Englischübersetzung ist unvollständig:

    * Freeplay:

      * Engine-Suchtiefe (auch im Lernmodus)
      * "Figur wählen und Feld anklicken. Ziehen verschiebt ..."
      * Weiß/Schwarz kurze/lange Rochade
      * "Figuren ziehen, um sie zu versetzen."
    * Puzzle:

      * Bei Einstellungen bei der Presets liste steht "Rätsel"
      * Generell die Parameter in der linken Spalte in den Einstellungen (Rätsel-Schwierigkeit, Zeit pro eigenem Zug, Zeit insgesamt, Leben)
      * Bei Endgames "Exakt bei kurzen Mattaufgaben und KPK-Umwandlung ..."
      * Learn from mistakes "Zuerst Partien im Bereich Eigene Spiele ..."
      * Punish opponent mistakes "Ausgelassene, schwache Gegnerzüge deines ..."
    * Einstellungen:

      * "Realer Test auf diesem PC: STellung nach e4 e5 ..."
      * "Engine Suchtiefe"
      * "Standardmäßig ordnet die verbesserte Positionierung Geschwister ...."
      * "Gemeinsames Enginebudget für Taktik, Endspiel, ..."
    * Branchmodus:

      * Stellungsvarianten
      * "Bearbeitung ohne Übernehmen verlassen ..."
      * "Lernkarten insgesamt"
    * Eigene SPiele \& Analyse:

      * "Besten Zug zeigen", "Selbst verbessern"
      * Diese kleine Info Bestzug, gespielt, Verlust, Gespeicherte Tiefe, ...
    * Schachblick:

      * Aufgabe x/30, Fehler ....
      * Nächste Aufgabe - Leertaste
      * "Rechts ist besser Unterschied Bauern laut Engine"
      * Statistik: Zeitraum, Durchgänge, Gesamte datierte Durchgänge, ..., die Legende
    * Generell wahrscheinlich auch beim Hovern von vielen Sachen
    * Wahrscheinlich gibt es noch viele mehr Stellungen
* Einstellungen:

  * Bei den Menüs bspw. in den Einstellungen, wenn ich meinen Mauszeiger bewege, ohne über etwas drüberzuhovern flackern die Inputkästen (bspw. die Menüs unter Sprache, Brett \& Figuren oder auch Suchtiefe: sofortige Bewertung).
* Lernmodus:

  * Füge einen Parameter hinzu Automatisches Engine Spiel (Standard false), aber entferne den Weiterspielen Knopf (Entsprechend auch die Entertastenfunktion entfernen) -> vertikal mehr Platz für Zughistory
* Freeplay:

  * Setze die Knöpfe genauso wie beim Lernmodus. Also Lösung zeigen unter dem Brett. In Branch zeigen dort lassen. Unter der Zughistory Platz für "Zur aktuellen Stellung" (wenn nicht auf aktuelle Stellung) \& "Anfangsstellung" (Wenn auf aktuelle Stellung) (als eine Zeile) und dadrunter "Ab hier weiterspielen" Knopf (wenn nicht auf aktuelle Stellung).
* Branchmodus:

  * Der Analysemodus laggt sehr stark (als im Vergleich zu Lernmodus, der funktioniert). Eigentlich sollte die Performance zwischen Analysemodus \& Lernmodus praktisch dieselbe sein.
* Freies Spiel:

  * Benenne den Parameter Engine spielt den Gegner um in Automatisches Engine Spiel
  * Passe das Board drehen Icon dem selben an, wie bei Lokales Spiel. Verschiebe ihn auf die rechte Spalte oben.
  * Passe dem Zur aktuellen Stellung Knopf so an, dass er im selben Stil ist, wie beim Lernmodus. -> Kohärenz
  * Ersetze den "Zug zurück" Knopf, durch den "Ab hier weiterspielen" Knopf (nur einblenden, wenn nicht auf aktuelle Stellung), kommt unter "Zur aktuellen Stellung" auf der rechten Spalte, lass auch permanent Platz frei unter Zughistory.
* COG:

  * Die Lernperspektive soll nicht über Scrollen umgestellt werden können.
  * Beim Scrollen braucht das Fenster ein bisschen zu lange bis es die UI richtig neumalt.
* Schachblick:

  * Du hast am Anfang von Figuren retten \& Figuren merken vergessen, wie bei den anderen Submodi, einen Bestätigungsknopf hinzuzufügen unten, wenn der Infokasten angezeigt wird."
* 18.10. 12:30: "Die Applikation ist jetzt nun vollkommen funktionsfähig.
Mach das Projekt jetzt GitHub-ready, sodass es später professionell, transparent, rechtlich sauber und möglichst vollständig Open Source veröffentlicht werden kann. (Gib das Projekt aus bzw. sag mir, wo ich es mir nochmal anschauen kann.) Mach dabei keine unnötigen architektonische Veränderungen, welche die Modularität des Projekts reduzieren.:
* sämtliche sichtbaren GitHub-Inhalte (v.a. README.md u.ä.) sind auf Englisch zu verfassen
* Bereite eine professionelle Repository-Struktur mit README.md, LICENSE, .gitignore, CONTRIBUTING.md, CHANGELOG.md, SECURITY.md, THIRD\_PARTY\_NOTICES.md, CITATION.cff und weiteren sinnvollen Dateien vor.
* Achte besonders darauf, dass das alles mit der Lizenz von der benutzten Stockfish-Version in Ordnung ist (falls dem nicht so sein sollte, führe in der Hinsicht Änderungen aus und mache dabei transparent, wie du A sicherstellst, dass das alles in Ordnung ist (-> zu viel sicherzustellen ist jetzt auch nicht nötig) und B, wenn nötig, was du genau geändert hast).
* Gestalte die README.md als übersichtliche GitHub-Startseite mit Projektbeschreibung, Funktionen, Installationsanleitung, Downloadmöglichkeit, Screenshots und Informationen für Entwickler. Erstelle passende Platzhalter, damit ich später eigene Screenshots und Beschreibungen ergänzen kann (und sag mir auch, wie ich das konkret machen kann).
* Mach transparent, dass das Projekt mit der Hilfe von Codex erstellt wurde, manche Icons habe ich über Figma erstellt und die Sounds über Audacity selber editiert stammen von Pixabay -> Überprüfe, ob da eine Quellenangabe o.ä. notwendig ist.
* Lege einen Ordner docs/ai-development/ an, in dem ich später selbst die KI-gestützte Entwicklung, verwendete Modelle, Prompts, Architekturentscheidungen und meinen eigenen Beitrag wissenschaftsnah dokumentieren kann. Bereite dafür sinnvolle Vorlagen vor, ohne Informationen zu erfinden.
* Das Einfügen der über 1000 Tests haben eine sekundäre Priorität (also noch nichts dafür für GItHub machen, nur evaluieren inwieweit das Einfügen über GitHub-Actions sinnvoll ist).
Erkläre mir wie ein normaler Windows-Endnutzer die Anwendung später über GitHub Releases herunterladen, installieren und starten würde. Gib mir aus wie die Anwendung beim Benutzer aussieht (am besten u.a. als .exe) indem du folgendes zusätzlich sicherstellst:
* Stelle sicher, dass keine persönlichen Daten von mir dort auftauchen (chess.com Benutzername, realer Name, ChatGPT credentials, Passwörter, ...).
* Stelle sicher, dass nicht meine eigenen Spiele in Eigene Spiele \& Analyse vorzufinden sind (füge nur ein simples mock-up game hinzu, in welchem schwarz so schnell wie möglich verliert -> kann man löschen).
* Bisher gibt es ja ein Opening, welches man nicht löschen kann und als Mock-Up Opening behandelt wird (namens "Pirc Defence – Schwarzes Repertoire, erweitert"). Dieses soll in der Endversion nicht mehr vorzufinden sein, und ersetze dieses stattdessen durch 2 andere, welche in der letzten Version von mir erstellt wurden "Example\_white: English\_Opening, depth=16" \& "Example\_black: Pirc\_Defense, depth\~=16?", Namen kannst du behalten, wie sie gerade sind.
* Bei Schachblick unter Statistik, gibt es ja eine Demo für die letzten 400 Tage, die Einstellung soll in den Einstellungen von Schachblick versteckt sein.
[@GitHub](plugin://github@openai-curated-remote) Ich habe das chess-trainer Repository erstellt. Prüfe, ob du zu diesem Repository Zugriff hast. Füge das Projekt noch nicht hinzu."
* 18.10. 17:41: "Arbeite daran weiter, was du nicht beenden konntest"
