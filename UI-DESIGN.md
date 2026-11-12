# Argos – operative UI-Designsprache

Dieses Designsystem beschreibt verbindlich die dargestellte Argos-Anwendung. Es vermittelt eine ruhige, präzise und belastbare Einsatzoberfläche: Informationshierarchie, Zustand und Handlungsfähigkeit stehen vor Dekoration. Allgemeine SaaS-, Dashboard- oder Karten-UI-Konventionen gelten nicht, wenn sie dieser Referenz widersprechen.

## Gestaltungsziel

- Eine Hauptaufgabe pro Ansicht. Der primäre Inhalt erhält die dominante Fläche; Werkzeuge bleiben nachrangig.
- Kritischer Zustand und wichtigste Information sind zuerst sichtbar, unterstützende Informationen folgen danach.
- Große ruhige Flächen und hohe Informationsdichte werden verbunden. Vollflächen mit Safe Areas sind gleichförmigen Rastern schwebender Flächen vorzuziehen.
- Zustände werden immer zusätzlich zur Farbe durch Text oder Symbol vermittelt.
- Keine dekorative, futuristische, verspielte oder SaaS-artige Optik verwenden.
- Die Anwendung wirkt wie ein technisches Lageinstrument: flächig, dunkel, monospaced, mit wenigen klaren Zustandsfarben und ohne visuelle Lautstärke.

## Themes und verbindliche Tokens

Alle Oberflächen unterstützen Dark, Light und Automatik. Automatik folgt der Systempräferenz und wendet dabei den vollständigen Dark- oder Light-Token-Satz an. Außerhalb dieser Tokens sind keine zusätzlichen Farben, Abstände, Radien, Schriftgrößen, Schatten oder Unschärfewerte zulässig.

```css
:root,
[data-theme="dark"] {
  --color-background: #080808;
  --color-surface: #101010;
  --color-surface-raised: #141414;
  --color-text-primary: #ECECEC;
  --color-text-secondary: #A0A0A0;
  --color-text-muted: #6F6F6F;
  --color-border: #303030;
  --color-border-subtle: #242424;
  --color-icon: #D8D8D8;
  --color-overlay-background: rgba(8, 8, 8, 0.84);
  --color-visual-base: #151515;
  --color-visual-detail: #242424;
  --color-code-keyword: #D7A84A;
  --color-code-type: #72B7E8;
  --color-code-member: #C99BE8;
  --color-code-string: #75C68A;
  --color-code-comment: #6F6F6F;

  --color-critical: #FF3B0A;
  --color-critical-surface: rgba(255, 59, 10, 0.20);
  --color-warning: #C99526;
  --color-positive: #16B83D;
  --color-active: #00B83A;

  --color-route: #FF2400;
  --color-location: #FF3B0A;
  --color-map-label: #8B8B8B;
  --color-map-road: #2A2A2A;
  --color-map-building: #1B1B1B;
}

[data-theme="light"] {
  --color-background: #F6F6F6;
  --color-surface: #FFFFFF;
  --color-surface-raised: #EBEBEB;
  --color-text-primary: #171717;
  --color-text-secondary: #606060;
  --color-text-muted: #707070;
  --color-border: #B8B8B8;
  --color-border-subtle: #D7D7D7;
  --color-icon: #252525;
  --color-overlay-background: rgba(246, 246, 246, 0.84);
  --color-visual-base: #EFEFEF;
  --color-visual-detail: #D6D6D6;
  --color-code-keyword: #805A00;
  --color-code-type: #075F98;
  --color-code-member: #734A92;
  --color-code-string: #087A29;
  --color-code-comment: #707070;

  --color-critical: #B72C08;
  --color-critical-surface: rgba(183, 44, 8, 0.16);
  --color-warning: #8A6400;
  --color-positive: #087A29;
  --color-active: #007A29;

  --color-route: #C92808;
  --color-location: #B72C08;
  --color-map-label: #666666;
  --color-map-road: #C7C7C7;
  --color-map-building: #E2E2E2;
}

:root {
--font-family-primary: "Cascadia Code", ui-monospace, monospace;
--font-wordmark-mobile: 48px;
--font-wordmark-desktop: 64px;
--font-hero-mobile: 48px;
--font-hero-desktop: 64px;
--font-metric-mobile: 32px;
--font-metric-desktop: 48px;
--font-section-title: 20px;
--font-body: 16px;
--font-meta: 14px;
--font-label: 12px;
--line-height-body: 1.5;
--line-height-compact: 1.35;
--letter-spacing-wordmark: 0.24em;
--letter-spacing-label: 0.12em;

--space-8: 8px;
--space-16: 16px;
--space-24: 24px;
--space-32: 32px;
--space-48: 48px;
--space-64: 64px;
--page-padding-mobile: 24px;
--page-padding-tablet: 32px;
--page-padding-desktop: 48px;

--border-width: 1px;
--radius-none: 0px;
--radius-overlay: 24px;
--overlay-blur: 16px;
--overlay-gradient-depth: 160px;
--transition-duration: 160ms;
--size-icon: 24px;
--size-target-min: 44px;
--stroke-emphasis: 6px;
}
```

`--color-active` kennzeichnet ausschließlich aktive Navigation und Auswahl. Der sichtbare Produktname bleibt neutral. `--color-route` kennzeichnet Bewegungsverläufe und `--color-location` den eigenen Standort, Zielmarker sowie kritische Daten. Warnung, Erfolg und Fehler behalten ausschließlich ihre Statusbedeutung.

## Typografie

Cascadia Code ist die primäre Schriftfamilie für die Benutzeroberfläche. Zahlen verwenden tabellarische Ziffern. Die typografische Wirkung ist technisch, leicht und luftig; große Werte werden nicht durch starke Schriftschnitte, sondern durch Größe und freien Raum hervorgehoben.

- Wortmarken und zentraler Systemname: `--font-wordmark-*`, Gewicht 400.
- Primäre Inhaltsbezeichnungen: `--font-hero-*`, Gewicht 400.
- Primäre Werte: `--font-metric-*`, Gewicht 500.
- Abschnittsüberschriften: `--font-section-title`, Gewicht 600, Großbuchstaben.
- Feldlabels: `--font-label`, Großbuchstaben, `--letter-spacing-label`, `--color-text-secondary`.
- Sekundäre Angaben: `--font-meta`, `--color-text-secondary`.
- Standardinhalt: `--font-body`, `--color-text-primary`.
- Gedämpfte oder sekundäre Inhalte: `--color-text-muted`.
- Keine Schriftgewichte über 600, keine unnötige Fettschrift und keine unnötig langen Textblöcke verwenden.

## Responsive Layout

- Mobil: bis einschließlich 767 px; einspaltig und nach Informationspriorität angeordnet.
- Tablet: 768 px bis einschließlich 1023 px; zwei Spalten im Verhältnis 2:1.
- Desktop: ab 1024 px; keine universelle 2:1-Teilung. Lage- und Sensoransichten nutzen ein 50:50-Grundraster. Die rechte Hälfte kann oben eine Karte über die volle Breite und unten zwei gleich breite Instrumentenfelder enthalten. Fahr- und Detailansichten dürfen eine große primäre Fläche mit nachgeordneten scharf getrennten Datenfeldern kombinieren.
- Eng verbundene Elemente: `--space-8`; innerhalb einer Komponente: `--space-16`; zwischen Komponenten: `--space-24`; zwischen Abschnitten: `--space-48`; zwischen Hauptbereichen: `--space-64`.
- Der primäre Inhalt erhält die größte verfügbare Fläche.
- Lange vertikale und horizontale Linien trennen Bereiche mit `--border-width solid --color-border`.
- Mobile Kartenansichten bleiben vollflächig. Informationen liegen als wenige, direkt auf die Karte bezogene HUD-Zonen am oberen oder unteren Rand; sie werden nicht in Karten oder schwebende Bedienfelder zerlegt.

## Oberflächen und Tiefe

- Standardhintergrund ist `--color-background`; erhöhte, nicht schwebende Bereiche verwenden `--color-surface`.
- Abgegrenzte Inhaltsbereiche sind rechteckig, transparent oder `--color-surface`, mit `--color-border` gerahmt. Sie werden nur für direkt vergleichbare Informationen verwendet.
- Schwebende Overlays verwenden ausschließlich für Uhrzeit- und Karten-HUDs `--radius-overlay`, `--color-overlay-background` und `--overlay-blur`. Ihre untere Kante darf über `--overlay-gradient-depth` in die Karte auslaufen. Alle anderen Bereiche bleiben scharf.
- Feine Divider sind sichtbaren Rahmen für allgemeine Layoutbereiche vorzuziehen. Vollständig deckende Flächen sind fokussierten Interaktionen vorbehalten.
- Dezente Verläufe sind ausschließlich erlaubt, um eine Overlay-Kante in einen dynamischen Hintergrund zu überführen. Sie dürfen Kontrast oder Lesbarkeit nicht beeinträchtigen.
- Keine dekorativen Schatten, keine Gradients außerhalb dieser Overlay-Überblendung, keine stark gerundeten Flächen, Pill-Buttons oder Glassmorphism-Komponenten verwenden.

## Komponenten

### Wiederholte Inhaltsbereiche

- Wiederholte Inhaltsbereiche besitzen mindestens `--space-24` vertikalen Innenabstand und sind durch `--color-border-subtle` getrennt.
- Sekundäre Angaben, primärer Inhalt und wichtigste Information erhalten eine klare visuelle Rangfolge.
- Zustände sind zusätzlich zur Farbe textlich oder durch ein Symbol benannt.
- Direkt vergleichbare Informationen nutzen `--space-24` Innenabstand, `--border-width solid --color-border` und `--radius-none`. Die wichtigste Information ist dominant; die Bezeichnung bleibt klein und gedämpft.

### Buttons, Navigation und Fokus

- Alle interaktiven Ziele sind mindestens `--size-target-min` × `--size-target-min` groß.
- Standardaktionen verwenden Text oder technische Linienicons in `--color-icon`; Icons sind `--size-icon` groß und ungefüllt.
- Sekundäre Buttons verwenden `--border-width solid --color-border`, transparenten Hintergrund und `--radius-none`.
- Schließen-, Zurück- und vergleichbare Sekundäraktionen sind standardmäßig flache Icon-Controls ohne permanente Hintergrundfläche und ohne Border. Eine umschlossene Variante erfordert eine Begründung durch Untergrund oder Sichtbarkeit.
- Destruktive oder kritische Aktionen verwenden `--color-critical` für Text oder Icon. Vollflächig kritisch ist nur eine ausdrücklich bestätigte irreversible Aktion.
- Untere Navigation hat vier bis sechs gleich breite Ziele; das aktive Ziel wird ausschließlich mit `--color-active` markiert.
- Auf wechselndem oder kontrastarmem Hintergrund erhalten Icon-Buttons eine neutrale, kontrastierende Fläche und bei Bedarf eine dezente Border. Auf ruhigen, einfarbigen oder klar abgegrenzten Flächen bleiben sie flach.
- Jede Icon-only-Aktion besitzt einen zugänglichen Namen. Alle interaktiven Ziele erhalten einen sichtbaren Fokus über `--color-active` und `--border-width`.
- Hover nutzt `--color-surface-raised`; Pressed nutzt `--color-border`; Selected nutzt `--color-active`. Deaktivierte Aktionen sind nicht ausführbar und nennen den Grund unmittelbar an der Aktion.

### Status und Visualisierungen

- Live oder positiv: `--color-positive`; aktive Auswahl oder Navigation: `--color-active`; Warnung: `--color-warning`; kritisch: `--color-critical`.
- Nicht verfügbar, veraltet, geschätzt und fehlerhaft werden immer explizit textlich bezeichnet. Kritische Bereiche dürfen `--color-critical-surface` nutzen.
- Komplexe Visualisierungen verwenden `--color-visual-base` und `--color-visual-detail`; Beschriftungen nutzen `--color-text-secondary`. Hervorgehobene Elemente liegen visuell über Hintergrund und Beschriftungen.
- Hervorgehobene Verbindungselemente verwenden genau eine semantische Farbe mit `--stroke-emphasis` Linienbreite.
- Datenvisualisierungen verwenden `--color-background`, Raster in `--color-border-subtle` und genau eine semantische Datenfarbe. Sekundäre Daten nutzen `--color-text-muted`; aktuelle oder kritische Daten nutzen `--color-critical`.

### Karte, Route und Lage-HUD

- Die Karte ist ein funktionaler, beinahe schwarzer Untergrund: Gebäude `--color-map-building`, Straßen `--color-map-road`, Beschriftungen `--color-map-label`. Sie darf nie hell, bunt oder topografisch dekorativ sein.
- Routen sind eine einzelne, kantige Polylinie in `--color-route` mit `--stroke-emphasis`; ein zurückhaltender dunkler Schatten darf ausschließlich die Lesbarkeit auf der Karte sichern. Es gibt keine Verlaufsroute und keine mehrfarbigen Segmente.
- Der eigene Standort ist ein `--color-location`-Punkt mit heller Kontur und, wenn Genauigkeit relevant ist, einem transparenten kritischen Radius. Ziele nutzen ein reduziertes Linienpiktogramm in derselben Farbe und eine kurze, gerade Verbindungslinie.
- Kartenbeschriftungen bleiben sichtbar, liegen aber stets hinter Route, Standort und HUD. Karteninhalte erhalten keine Kachelrahmen.
- Eine zentrale Uhr darf oben als breite, dunkle HUD-Fläche mit ausgeprägten oberen Ecken erscheinen. In der mobilen Kartenansicht steht die Wortmarke zentriert darüber oder im ruhigen Kopfbereich.
- Untere Karten-HUDs bestehen aus Textzeilen, kleiner Höhenprofilgrafik, Maßstab und einer scharf abgegrenzten Statusleiste. Die Statusleiste hat vier bis sechs gleich breite Zellen; nur der aktive Zustand ist grün.

### Instrumentenansichten

- Sensor-, Kompass- und Lageansichten folgen dem Desktop-50:50-Raster und großflächigen, randlosen Analyseflächen.
- Instrumente sind als feine Liniengrafiken in neutralem Grau ausgeführt. Kritische Messwerte, Marker und Profile verwenden Rot/Orange, niemals Grün.
- Nicht verfügbare Sensoren lassen ihre Fläche leer und zeigen mittig eine kurze, konkrete Ursache sowie gegebenenfalls eine kantige sekundäre Aktion. Es gibt keine Platzhalterillustrationen oder Skeletons.

## Interaktion, Bewegung und Operationalität

- Standardtransition: `--transition-duration ease-out`. Es gibt keine federnden, dekorativen oder dauerhaften Animationen.
- Bei `prefers-reduced-motion` sind Transitionen deaktiviert.
- Kontinuierlich aktualisierte Werte werden nur zwischen gültigen Zeitstempeln zeitbasiert interpoliert. Zustandswechsel und Warnungen werden nie interpoliert.
- Automatisch aktualisierte Inhalte und Interpolation laufen nur auf sichtbaren, aktiven Flächen.
- Lade-, Verbindungs-, Fehler- und Aktualisierungszustände sind sichtbar und textlich eindeutig.

## Prüfung und Governance

- Jede Erweiterung wird in Dark, Light und Automatik geprüft, einschließlich Kontrast, Statusverständlichkeit und ausschließlich tokenbasierter Werte.
- Prüfe die Breakpoints 767, 768, 1023 und 1024 px, kleinen Touchscreen, Tastaturfokus, Verbindungs-/Fehlerzustände, Icon-Buttons auf ruhigen und wechselnden Hintergründen sowie `prefers-reduced-motion`.
- Prüfe Livewert-Interpolation mit gültigen und fehlenden Zeitstempeln sowie Zustandswechseln.
- Neue visuelle Werte, Tokens oder Komponentenvarianten dürfen nur verwendet werden, nachdem sie als verbindlicher Token oder dokumentierte Komponente in dieser Datei ergänzt wurden.
