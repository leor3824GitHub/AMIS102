Report assets for QuestPDF government forms.

Drop the agency logo here to have it printed on the Disbursement Voucher header:

    ReportAssets\nfa-logo.png   (any name containing "logo" is preferred)

Rules:
- Format must be a RASTER image: PNG (recommended, transparent background) or JPG.
  SVG is NOT supported by QuestPDF's .Image() — convert to PNG first.
- Roughly square works best (the header reserves a 60 x 48 pt box and scales to fit).
- ~200x200 px or larger keeps it crisp.
- Files matching ReportAssets\**\*.png and ReportAssets\**\*.jpg are embedded automatically
  (see Modules.QuestPdfReporting.csproj). No code change needed after adding the file —
  just rebuild.

If no image is present here, the header simply prints the agency text without a logo.
