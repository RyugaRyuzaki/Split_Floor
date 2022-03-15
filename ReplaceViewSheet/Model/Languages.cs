using System.Collections.Generic;
using WpfCustomControls;

namespace ReplaceViewSheet
{
    public class Languages:BaseViewModel
    {
        private List<string> _AllLanguages;
        public List<string> AllLanguages { get { if (_AllLanguages == null) { _AllLanguages = new List<string>() { "EN", "VN" }; } return _AllLanguages; } set { _AllLanguages = value; OnPropertyChanged(); } }

        private string _SelectedLanguage;
        public string SelectedLanguage { get { return _SelectedLanguage; } set { _SelectedLanguage = value; OnPropertyChanged(); } }

        private string _SearchBySheetNumber;
        public string SearchBySheetNumber { get { return _SearchBySheetNumber; } set { _SearchBySheetNumber = value; OnPropertyChanged(); } }
        private string _PrefixSheetNumber;
        public string PrefixSheetNumber { get { return _PrefixSheetNumber; } set { _PrefixSheetNumber = value; OnPropertyChanged(); } }

        private string _Up;
        public string Up { get { return _Up; } set { _Up = value; OnPropertyChanged(); } }
        private string _Down;
        public string Down { get { return _Down; } set { _Down = value; OnPropertyChanged(); } }
        private string _Insert;
        public string Insert { get { return _Insert; } set { _Insert = value; OnPropertyChanged(); } }
        private string _Delete;
        public string Delete { get { return _Delete; } set { _Delete = value; OnPropertyChanged(); } }
        private string _Replace;
        public string Replace { get { return _Replace; } set { _Replace = value; OnPropertyChanged(); } }


        private string _SheetNumber;
        public string SheetNumber { get { return _SheetNumber; } set { _SheetNumber = value; OnPropertyChanged(); } }
        private string _SheetName;
        public string SheetName { get { return _SheetName; } set { _SheetName = value; OnPropertyChanged(); } }
        private string _TitleBlock;
        public string TitleBlock { get { return _TitleBlock; } set { _TitleBlock = value; OnPropertyChanged(); } }
        private string _ViewNamesInSheet;
        public string ViewNamesInSheet { get { return _ViewNamesInSheet; } set { _ViewNamesInSheet = value; OnPropertyChanged(); } }
        public Languages(string language)
        {
            SelectedLanguage = language;
            ChangedLanguage();

        }
        public void ChangedLanguage()
        {
            switch (SelectedLanguage)
            {
                case "EN": GetLanguageEN(); break;
                case "VN": GetLanguageVN(); break;
                default: GetLanguageEN(); break;
            }
        }
        private void GetLanguageEN()
        {
            SearchBySheetNumber = "Search By Sheet Number";
            PrefixSheetNumber = "Prefix-Sheet Number";
            Up = "Up";
            Down = "Down";
            Insert = "Insert";
            Delete = "Delete";
            Replace = "Replace";
            SheetNumber = "Sheet Number";
            SheetName = "Sheet Name";
            TitleBlock = "Title Block";
            ViewNamesInSheet = "View Names In Sheet";
        }
        private void GetLanguageVN()
        {
            SearchBySheetNumber = "Tìm kiếm theo Số hiệu bản vẽ";
            PrefixSheetNumber = "Tiền tố số hiệu bản vẽ";
            Up = "Lên trên";
            Down = "Xuống dưới";
            Insert = "Thêm mới";
            Delete = "Xoá";
            Replace = "Thay thế";
            SheetNumber = "Số hiệu bản vẽ";
            SheetName = "Tên Bản vẽ";
            TitleBlock = "Khung tên";
            ViewNamesInSheet = "Tên các view trong bản vẽ";
        }
    }
}
