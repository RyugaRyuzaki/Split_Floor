
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using WpfCustomControls;

namespace ReplaceViewSheet
{
    public class ViewSheetModel : BaseViewModel
    {
        private static readonly BuiltInCategory TitleBlockCategory = (BuiltInCategory)(-2000280);
      
        private static readonly BuiltInParameter SheetNumberID = (BuiltInParameter)(-1007401);
        private string _SheetName;
        public string SheetName { get { return _SheetName; } set { _SheetName = value; OnPropertyChanged(); } }

        private string _SheetNumber;
        public string SheetNumber { get { return _SheetNumber; } set { _SheetNumber = value; OnPropertyChanged(); } }

        private ViewSheet _ViewSheet;
        public ViewSheet ViewSheet { get { return _ViewSheet; } set { _ViewSheet = value; OnPropertyChanged(); } }
        private ObservableCollection<FamilySymbol> _AllFamilySymbols;
        public ObservableCollection<FamilySymbol> AllFamilySymbols { get { if (_AllFamilySymbols == null) _AllFamilySymbols = new ObservableCollection<FamilySymbol>(); return _AllFamilySymbols; } set { _AllFamilySymbols = value; OnPropertyChanged(); } }

        private FamilySymbol _SelectedFamilySymbol;
        public FamilySymbol SelectedFamilySymbol { get { return _SelectedFamilySymbol; } set { _SelectedFamilySymbol = value; OnPropertyChanged(); } }
        private Element _TitleBlock;
        public Element TitleBlock { get { return _TitleBlock; } set { _TitleBlock = value; OnPropertyChanged(); } }

        public ViewSheetModel(Document document, ViewSheet viewSheet)
        {
            ViewSheet = viewSheet;
            SheetName = ViewSheet.Name;
            SheetNumber = ViewSheet.SheetNumber;
            GetAllFamilySymbol(document);
            SelectedFamilySymbol= GetSelectedFamilySymbol(document);
            

        }
        public ViewSheetModel(Document document, ViewSheetModel viewSheetModel)
        {
            using (Transaction transaction=new Transaction(document))
            {
                transaction.Start("Dupplicate");
                try
                {
                    ViewSheet = ViewSheet.Create(document, viewSheetModel.SelectedFamilySymbol.Id);
                    ViewSheet.Name = viewSheetModel.SheetName + "-Copy";
                    ViewSheet.SheetNumber = viewSheetModel.SheetNumber + "-Copy";
                    SheetName = ViewSheet.Name;
                    SheetNumber = ViewSheet.SheetNumber;
                    GetAllFamilySymbol(document);
                    SelectedFamilySymbol= GetSelectedFamilySymbol(document);
                  
                }
                catch (Exception e)
                {

                    System.Windows.Forms.MessageBox.Show(e.Message);
                }
               
                transaction.Commit();
            }
        }
        public void DeletedViewSheet(Document document)
        {
            using (Transaction transaction = new Transaction(document))
            {
                transaction.Start("Delete ViewSheet");
                try
                {
                    IEnumerable<ElementId> elementIds = document.Delete(ViewSheet.Id);
                }
                catch (Exception e)
                {

                    System.Windows.Forms.MessageBox.Show(e.Message);
                }

                transaction.Commit();
            }
        }
        
        private void GetAllFamilySymbol(Document document)
        {

            List<Family> families = new FilteredElementCollector(document).OfClass(typeof(Family)).Cast<Family>().Where(x => x.FamilyCategory.Name.Equals(Category.GetCategory(document, TitleBlockCategory).Name)).ToList();

            if (families.Count != 0)
            {
                foreach (var item in families)
                {
                    foreach (var item1 in item.GetFamilySymbolIds())
                    {
                        FamilySymbol familySymbol = item.Document.GetElement(item1) as FamilySymbol;
                        AllFamilySymbols.Add(familySymbol);
                    }
                }
            }
        }
        public FamilySymbol GetSelectedFamilySymbol(Document document)
        {
            FamilySymbol familySymbol = null;
            List<Element> elements = new FilteredElementCollector(document).OwnedByView(ViewSheet.Id).ToList();
            foreach (Element el in elements)
            {
                foreach (FamilySymbol Fs in AllFamilySymbols)
                {
                    if (el.GetTypeId().IntegerValue == Fs.Id.IntegerValue)
                    {
                        familySymbol = Fs as FamilySymbol;
                    }
                }
            }
            return familySymbol;
        }
        private void ReplacePreFixSheetNumberItem(Document document, string prefix,string number)
        {
            SheetNumber = prefix + number;
            ViewSheet.get_Parameter(SheetNumberID).Set(SheetNumber);
           
        }
        public static ObservableCollection<ViewSheetModel> GetAllViewSheetModels(Document document, ObservableCollection<ViewSheet> ViewSheets, string searchText)
        {
            ObservableCollection<ViewSheetModel> viewSheetModels = new ObservableCollection<ViewSheetModel>();
            foreach (var item in ViewSheets)
            {
                if (String.IsNullOrEmpty(searchText))
                {
                    viewSheetModels.Add(new ViewSheetModel(document, item));
                }
                else
                {
                    if (item.SheetNumber.ToUpper().Contains(searchText.ToUpper())) viewSheetModels.Add(new ViewSheetModel(document, item));
                }
            }
            return new ObservableCollection<ViewSheetModel>(viewSheetModels.OrderBy(x => x.SheetNumber));
        }
        public static void ReplacePreFixSheetNumber(Document document, ObservableCollection<ViewSheetModel> searchViewSheetModels, string prefix)
        {
            int total = searchViewSheetModels.Count;
            using (Transaction transaction = new Transaction(document))
            {
                transaction.Start("Replace ViewSheet Number");
                try
                {
                    int i = 1;
                    while (i <= total)
                    {
                        string Zero = GetNumberZeroOfNumber(total, i);
                        string number = Zero + i.ToString();
                        searchViewSheetModels[i - 1].ReplacePreFixSheetNumberItem(document, prefix, number);
                        i++;
                    }
                }
                catch (Exception e)
                {

                    System.Windows.Forms.MessageBox.Show(e.Message);
                }

                transaction.Commit();
            }
        }
        private static string GetNumberZeroOfNumber(int total, int number)
        {
            int lenght = total.ToString().Length;
            int i = 1;
            int number1 = number;
            while (number1 / 10 > 0)
            {
                number1 /= 10;
                i++;
            }
            string zero = "";
            for (int j = 0; j < lenght - i; j++)
            {
                zero += "0";
            }
            return zero;
        }

    }

}
