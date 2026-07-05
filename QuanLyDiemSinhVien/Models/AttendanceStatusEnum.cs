namespace QuanLyDiemSinhVien.Models
{
	public enum AttendanceStatusEnum
	{
		NotMarked = 0, // Chưa điểm danh
		Present = 1,   // Có mặt
		Excused = 2,   // Vắng phép
		Unexcused = 3, // Vắng không phép
		Late = 4       // Muộn
	}
}
