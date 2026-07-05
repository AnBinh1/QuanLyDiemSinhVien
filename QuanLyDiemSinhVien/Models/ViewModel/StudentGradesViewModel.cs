namespace QuanLyDiemSinhVien.Models.ViewModel
{
	public class StudentGradesViewModel
	{
		public StudentModel Student { get; set; }
		public List<StudentGradeModel> Grades { get; set; } = new List<StudentGradeModel>();
	}
}
