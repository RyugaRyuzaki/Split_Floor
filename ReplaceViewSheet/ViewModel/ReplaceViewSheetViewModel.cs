#region Namespaces
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using WpfCustomControls;
using WpfCustomControls.ViewModel;
using View = Autodesk.Revit.DB.View;
#endregion

namespace ReplaceViewSheet
{
    public class ReplaceViewSheetViewModel : BaseViewModel
    {
        private static readonly BuiltInParameter TypeID = (BuiltInParameter)(-1002050);
        private static readonly BuiltInCategory TitleBlockCategory = (BuiltInCategory)(-2000280);
        public UIDocument UiDoc;
        public Document Doc;


        
        private ObservableCollection<ViewSheetModel> _SearchViewSheetModels;
        public ObservableCollection<ViewSheetModel> SearchViewSheetModels { get { if (_SearchViewSheetModels == null) _SearchViewSheetModels = new ObservableCollection<ViewSheetModel>(); return _SearchViewSheetModels; } set { _SearchViewSheetModels = value;
                if (SearchViewSheetModels.Count != 0)
                {
                    SelectedViewSheetModel = SearchViewSheetModels[0];
                                 
                }
                OnPropertyChanged(); } }

        private ObservableCollection<ViewSheet> _ViewSheets;
        public ObservableCollection<ViewSheet> ViewSheets { get { if (_ViewSheets == null) _ViewSheets = new ObservableCollection<ViewSheet>(); return _ViewSheets; } set { _ViewSheets = value;

                if (ViewSheets.Count!=0)
                {
                    SearchViewSheetModels = ViewSheetModel.GetAllViewSheetModels(Doc,ViewSheets, SearchText);

                }

                OnPropertyChanged(); } }
        private string _SearchText;
        public string SearchText
        {
            get { return _SearchText; }
            set
            {
                _SearchText = value;
                SearchViewSheetModels = ViewSheetModel.GetAllViewSheetModels(Doc, ViewSheets, SearchText);
                OnPropertyChanged();
            }
        }

        private string _PreFixSheetNumber;
        public string PreFixSheetNumber { get { return _PreFixSheetNumber; } set { _PreFixSheetNumber = value; OnPropertyChanged(); } }

        private ViewSheetModel _SelectedViewSheetModel;
        public ViewSheetModel SelectedViewSheetModel { get { return _SelectedViewSheetModel; } set { _SelectedViewSheetModel = value;
                if (SelectedViewSheetModel != null)
                {
                    Views = GetAllPlaceViews();
                }
                OnPropertyChanged(); } }

       

        private ObservableCollection<View> _Views;
        public ObservableCollection<View> Views { get { if (_Views == null) _Views = new ObservableCollection<View>(); return _Views; } set { _Views = value; OnPropertyChanged(); } }
        #region Icommand
        public ICommand CloseWindowCommand { get; set; }
        public ICommand SelectionLanguageChangedCommand { get; set; }

        public ICommand ReplaceCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand InsertCommand { get; set; }
        public ICommand DownCommand { get; set; }
        public ICommand UpCommand { get; set; }

        #endregion
        private TaskBarViewModel _TaskBarViewModel;
        public TaskBarViewModel TaskBarViewModel { get { return _TaskBarViewModel; } set { _TaskBarViewModel = value; OnPropertyChanged(); } }
        private Languages _Languages;
        public Languages Languages { get { return _Languages; } set { _Languages = value; OnPropertyChanged(); } }
        public ReplaceViewSheetViewModel(UIDocument uiDoc, Document doc)
        {
            UiDoc = uiDoc;
            Doc = doc;
            GetAllViewSheets();
            Languages = new Languages("EN");
            TaskBarViewModel = new TaskBarViewModel();
            #region Command
            UpCommand = new RelayCommand<ReplaceViewSheetWindow>((p) => { return SelectedViewSheetModel != null&&SearchViewSheetModels.IndexOf(SelectedViewSheetModel)!=0; }, (p) =>
            {
                SearchViewSheetModels.Move(SearchViewSheetModels.IndexOf(SelectedViewSheetModel), SearchViewSheetModels.IndexOf(SelectedViewSheetModel) - 1);
            });
            DownCommand = new RelayCommand<ReplaceViewSheetWindow>((p) => { return SelectedViewSheetModel != null && SearchViewSheetModels.IndexOf(SelectedViewSheetModel) != SearchViewSheetModels.Count-1; }, (p) =>
            {
                SearchViewSheetModels.Move(SearchViewSheetModels.IndexOf(SelectedViewSheetModel), SearchViewSheetModels.IndexOf(SelectedViewSheetModel)+ 1);
            });
            InsertCommand = new RelayCommand<ReplaceViewSheetWindow>((p) => { return SelectedViewSheetModel != null ; }, (p) =>
            {
                var a = new ViewSheetModel(Doc, SelectedViewSheetModel);

                SearchViewSheetModels.Insert(SearchViewSheetModels.IndexOf(SelectedViewSheetModel)+1, a);
            });
            DeleteCommand = new RelayCommand<ReplaceViewSheetWindow>((p) => { return SelectedViewSheetModel != null ; }, (p) =>
            {
                SelectedViewSheetModel.DeletedViewSheet(Doc);
                SearchViewSheetModels.Remove(SelectedViewSheetModel);
            });
            ReplaceCommand = new RelayCommand<ReplaceViewSheetWindow>((p) => { return !String.IsNullOrEmpty(PreFixSheetNumber) ; }, (p) =>
            {
                ViewSheetModel.ReplacePreFixSheetNumber(Doc, SearchViewSheetModels, PreFixSheetNumber);
            });
            CloseWindowCommand = new RelayCommand<ReplaceViewSheetWindow>((p) => { return true; }, (p) =>
            {
               
                if (!SameSheetName())
                {
                    MessageBox.Show("Can not rename Same Sheet Name");
                }
                else
                {
                    using(Transaction transaction= new Transaction(Doc))
                    {
                        transaction.Start("Rename Sheet");
                        for (int i = 0; i < SearchViewSheetModels.Count; i++)
                        {
                            if (!SearchViewSheetModels[i].SheetName.Equals(SearchViewSheetModels[i].ViewSheet.Name)) SearchViewSheetModels[i].ViewSheet.Name = SearchViewSheetModels[i].SheetName;
                            if (!SearchViewSheetModels[i].SheetNumber.Equals(SearchViewSheetModels[i].ViewSheet.SheetNumber)) SearchViewSheetModels[i].ViewSheet.SheetNumber = SearchViewSheetModels[i].SheetNumber;
                            FamilySymbol familySymbol = SearchViewSheetModels[i].GetSelectedFamilySymbol(Doc);
                            
                            if (SearchViewSheetModels[i].SelectedFamilySymbol.Id.IntegerValue != familySymbol.Id.IntegerValue)
                            {
                                try
                                {
                                    Element TitleBlock = new FilteredElementCollector(Doc).OwnedByView(SearchViewSheetModels[i].ViewSheet.Id).WhereElementIsNotElementType().OfCategory(TitleBlockCategory).Cast<Element>().FirstOrDefault();
                                    
                                    TitleBlock.get_Parameter(TypeID).Set(SearchViewSheetModels[i].SelectedFamilySymbol.Id);
                                }
                                catch (Exception e)
                                {

                                    MessageBox.Show(e.Message);
                                }
                               
                            }
                        }
                        transaction.Commit();
                    }
                    p.DialogResult = true;
                }

        

            });
            SelectionLanguageChangedCommand = new RelayCommand<ReplaceViewSheetWindow>((p) => { return true; }, (p) =>
            {
                Languages.ChangedLanguage();
            });
            #endregion
        }
        private void GetAllViewSheets()
        {
            ViewSheets = new ObservableCollection<ViewSheet>(new FilteredElementCollector(Doc).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().ToList());
        }
        private ObservableCollection<View> GetAllPlaceViews()
        {
            ObservableCollection<View> views = new ObservableCollection<View>();
            foreach (var item in SelectedViewSheetModel.ViewSheet.GetAllPlacedViews())
            {
                views.Add(Doc.GetElement(item) as View);
            }
            return views;
        }
        private ObservableCollection<ElementType> FilterWallTypeUserUsed(List<Wall> walls, List<ElementType> wallTypes0)
        {
            // đầu tiên thay thế list walltype mới;
            ObservableCollection<ElementType> wallTypes =new ObservableCollection<ElementType>( wallTypes0);
            ObservableCollection<ElementType> wallTypes1 =new ObservableCollection<ElementType>( wallTypes0);
            // chạy vòng lập các walltype mới
            for (int i = 0; i < wallTypes.Count; i++)
            {
                // tìm kiếm wallReplace trong list walls nếu có walltype của từng wall == walltype
               
                List<Wall> wallReplace = walls.Where(x => x.WallType.Id.IntegerValue == wallTypes[i].Id.IntegerValue).ToList();
                // nếu ko tồn tại wallReplace nào thì remove phần tử đó đi
                if (wallReplace.Count == 0) wallTypes1.Remove(wallTypes[i]);
            }
            return wallTypes1;
        }
       
        private bool SameSheetName()
        {
            for (int i = 0; i < SearchViewSheetModels.Count; i++)
            {
                ObservableCollection<ViewSheetModel> viewSheetModels = new ObservableCollection<ViewSheetModel>(SearchViewSheetModels.Where(x => x.SheetName.Equals(SearchViewSheetModels[i].SheetName)));
                if (viewSheetModels.Count > 1) return false;
            }
            return true;
        }

    }
}
