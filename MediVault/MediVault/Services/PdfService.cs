using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Xps.Packaging;
using MediVault.Models;
using Microsoft.Win32;

namespace MediVault.Services;

public static class PdfService
{
    public static bool ExportPrescription(Prescription rx)
    {
        var dlg = new SaveFileDialog
        {
            Title = "Save Prescription",
            Filter = "XPS Document (*.xps)|*.xps",
            FileName = $"{rx.Code}.xps"
        };
        if (dlg.ShowDialog() != true) return false;

        var doc = BuildPrescriptionFlowDocument(rx);

        try
        {
            if (File.Exists(dlg.FileName)) File.Delete(dlg.FileName);
            using (var xps = new XpsDocument(dlg.FileName, FileAccess.ReadWrite))
            {
                var writer = System.Windows.Xps.Packaging.XpsDocument.CreateXpsDocumentWriter(xps);
                var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
                writer.Write(paginator);
            }
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save: {ex.Message}", "Export error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    public static bool PrintPrescription(Prescription rx)
    {
        var dlg = new System.Windows.Controls.PrintDialog();
        if (dlg.ShowDialog() != true) return false;

        var doc = BuildPrescriptionFlowDocument(rx);
        doc.PageHeight = dlg.PrintableAreaHeight;
        doc.PageWidth = dlg.PrintableAreaWidth;
        doc.PagePadding = new Thickness(50);

        var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
        dlg.PrintDocument(paginator, $"Prescription {rx.Code}");
        return true;
    }

    private static FlowDocument BuildPrescriptionFlowDocument(Prescription rx)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 13,
            PagePadding = new Thickness(50),
            ColumnGap = 0,
            ColumnWidth = 9999
        };

        var header = new Paragraph
        {
            FontSize = 26,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x1F, 0x6F, 0xEB))
        };
        header.Inlines.Add(new Run("MediVault"));
        header.Inlines.Add(new Run("  •  Digital Prescription") { FontSize = 14, FontWeight = FontWeights.Light, Foreground = Brushes.Gray });
        doc.Blocks.Add(header);

        var meta = new Paragraph { Margin = new Thickness(0, 0, 0, 12) };
        meta.Inlines.Add(new Run($"Prescription Code: ") { FontWeight = FontWeights.SemiBold });
        meta.Inlines.Add(new Run($"{rx.Code}\n"));
        meta.Inlines.Add(new Run($"Issued On: ") { FontWeight = FontWeights.SemiBold });
        meta.Inlines.Add(new Run($"{rx.IssuedOn:dddd, MMMM d, yyyy  HH:mm}"));
        doc.Blocks.Add(meta);

        doc.Blocks.Add(BuildSeparator());

        var twoCol = new Section();
        var t = new Table();
        t.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        t.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        var trg = new TableRowGroup();
        var tr = new TableRow();

        var patientCell = new TableCell();
        patientCell.Blocks.Add(MakeHeader("Patient"));
        patientCell.Blocks.Add(MakeKv("Name", rx.Patient.FullName));
        patientCell.Blocks.Add(MakeKv("Medical ID", rx.Patient.MedicalId));
        patientCell.Blocks.Add(MakeKv("Age / Gender", $"{rx.Patient.Age} / {rx.Patient.Gender}"));
        patientCell.Blocks.Add(MakeKv("Phone", rx.Patient.Phone));
        if (!string.IsNullOrWhiteSpace(rx.Patient.Allergies))
            patientCell.Blocks.Add(MakeKv("Allergies", rx.Patient.Allergies!));

        var doctorCell = new TableCell();
        doctorCell.Blocks.Add(MakeHeader("Prescribing Doctor"));
        doctorCell.Blocks.Add(MakeKv("Name", rx.Doctor.FullName));
        doctorCell.Blocks.Add(MakeKv("Specialty", rx.Doctor.Specialty));
        if (!string.IsNullOrWhiteSpace(rx.Doctor.LicenseNumber))
            doctorCell.Blocks.Add(MakeKv("License", rx.Doctor.LicenseNumber!));
        if (!string.IsNullOrWhiteSpace(rx.Doctor.Phone))
            doctorCell.Blocks.Add(MakeKv("Phone", rx.Doctor.Phone!));

        tr.Cells.Add(patientCell);
        tr.Cells.Add(doctorCell);
        trg.Rows.Add(tr);
        t.RowGroups.Add(trg);
        twoCol.Blocks.Add(t);
        doc.Blocks.Add(twoCol);

        doc.Blocks.Add(BuildSeparator());

        if (!string.IsNullOrWhiteSpace(rx.Diagnosis))
        {
            doc.Blocks.Add(MakeHeader("Diagnosis"));
            doc.Blocks.Add(new Paragraph(new Run(rx.Diagnosis)) { Margin = new Thickness(0, 2, 0, 12) });
        }

        doc.Blocks.Add(MakeHeader("Rx — Medications"));

        var rxTable = new Table { CellSpacing = 0 };
        rxTable.Columns.Add(new TableColumn { Width = new GridLength(0.4, GridUnitType.Star) });
        rxTable.Columns.Add(new TableColumn { Width = new GridLength(2, GridUnitType.Star) });
        rxTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        rxTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        rxTable.Columns.Add(new TableColumn { Width = new GridLength(0.7, GridUnitType.Star) });
        rxTable.Columns.Add(new TableColumn { Width = new GridLength(0.5, GridUnitType.Star) });

        var headerGroup = new TableRowGroup();
        var headerRow = new TableRow { Background = new SolidColorBrush(Color.FromRgb(0xEE, 0xF3, 0xFC)) };
        headerRow.Cells.Add(MakeCell("#", true));
        headerRow.Cells.Add(MakeCell("Medication", true));
        headerRow.Cells.Add(MakeCell("Dosage", true));
        headerRow.Cells.Add(MakeCell("Frequency", true));
        headerRow.Cells.Add(MakeCell("Duration", true));
        headerRow.Cells.Add(MakeCell("Qty", true));
        headerGroup.Rows.Add(headerRow);
        rxTable.RowGroups.Add(headerGroup);

        var bodyGroup = new TableRowGroup();
        int idx = 1;
        foreach (var item in rx.Items)
        {
            var row = new TableRow();
            row.Cells.Add(MakeCell(idx.ToString()));
            var medName = $"{item.Medication.Name} {item.Medication.Strength}";
            if (!string.IsNullOrWhiteSpace(item.Medication.Form)) medName += $" ({item.Medication.Form})";
            row.Cells.Add(MakeCell(medName, false, true));
            row.Cells.Add(MakeCell(item.Dosage));
            row.Cells.Add(MakeCell(item.Frequency));
            row.Cells.Add(MakeCell(item.Duration));
            row.Cells.Add(MakeCell(item.Quantity.ToString()));
            bodyGroup.Rows.Add(row);

            if (!string.IsNullOrWhiteSpace(item.Instructions))
            {
                var instr = new TableRow();
                var c = new TableCell { ColumnSpan = 6 };
                c.Blocks.Add(new Paragraph(new Run($"Instructions: {item.Instructions}"))
                {
                    FontStyle = FontStyles.Italic,
                    Foreground = Brushes.DimGray,
                    Margin = new Thickness(8, 0, 0, 6)
                });
                instr.Cells.Add(c);
                bodyGroup.Rows.Add(instr);
            }
            idx++;
        }
        rxTable.RowGroups.Add(bodyGroup);
        doc.Blocks.Add(rxTable);

        if (!string.IsNullOrWhiteSpace(rx.Notes))
        {
            doc.Blocks.Add(BuildSeparator());
            doc.Blocks.Add(MakeHeader("Additional Notes"));
            doc.Blocks.Add(new Paragraph(new Run(rx.Notes)) { Margin = new Thickness(0, 2, 0, 12) });
        }

        doc.Blocks.Add(BuildSeparator());

        var footer = new Paragraph { Margin = new Thickness(0, 30, 0, 0) };
        footer.Inlines.Add(new Run("Prescribing Physician: ") { FontWeight = FontWeights.SemiBold });
        footer.Inlines.Add(new Run($"{rx.Doctor.FullName}\n"));
        footer.Inlines.Add(new Run("Signature: ___________________________") { Foreground = Brushes.DimGray });
        doc.Blocks.Add(footer);

        var disclaimer = new Paragraph(new Run("This prescription was generated electronically by MediVault. Please verify with the prescribing physician for any clarifications."))
        {
            FontSize = 10,
            Foreground = Brushes.Gray,
            Margin = new Thickness(0, 30, 0, 0)
        };
        doc.Blocks.Add(disclaimer);

        return doc;
    }

    private static BlockUIContainer BuildSeparator() =>
        new BlockUIContainer(new Border
        {
            Height = 1,
            Background = new SolidColorBrush(Color.FromRgb(0xDD, 0xE3, 0xEA)),
            Margin = new Thickness(0, 8, 0, 8)
        });

    private static Paragraph MakeHeader(string text) =>
        new Paragraph(new Run(text))
        {
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x1F, 0x6F, 0xEB)),
            Margin = new Thickness(0, 6, 0, 4)
        };

    private static Paragraph MakeKv(string key, string value)
    {
        var p = new Paragraph { Margin = new Thickness(0, 1, 0, 1) };
        p.Inlines.Add(new Run($"{key}: ") { Foreground = Brushes.Gray });
        p.Inlines.Add(new Run(value));
        return p;
    }

    private static TableCell MakeCell(string text, bool isHeader = false, bool bold = false)
    {
        var cell = new TableCell
        {
            Padding = new Thickness(8, 6, 8, 6),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xE3, 0xE9, 0xF2)),
            BorderThickness = new Thickness(0, 0, 0, 1)
        };
        var run = new Run(text);
        if (isHeader)
        {
            run.FontWeight = FontWeights.SemiBold;
            run.Foreground = new SolidColorBrush(Color.FromRgb(0x1F, 0x6F, 0xEB));
        }
        else if (bold) run.FontWeight = FontWeights.SemiBold;
        cell.Blocks.Add(new Paragraph(run) { Margin = new Thickness(0) });
        return cell;
    }
}
