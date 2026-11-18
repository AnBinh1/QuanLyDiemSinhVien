using Microsoft.EntityFrameworkCore;
using QuanLyDiemSinhVien.Models.ViewModel;
using QuanLyDiemSinhVien.Repository;

namespace QuanLyDiemSinhVien.Services
{
    public static class RankHelper
    {
        public static string GetRank(int score) => score switch
        {
            >= 90 => "Xuất sắc",
            >= 80 => "Tốt",
            >= 65 => "Khá",
            >= 50 => "Trung bình",
            >= 35 => "Yếu",
            _ => "Kém"
        };
    }

    public class ConductScoreService
    {
        private readonly DataContext _db;
        public ConductScoreService(DataContext db) => _db = db;

        private static int Round(double v) => (int)Math.Round(v, MidpointRounding.AwayFromZero);

        public async Task<List<ConductScoreRowViewModel>> GetScoreTableAsync(int studentId)
        {
            var list = await _db.ConductScores
                .Include(c => c.Semester)
                .Where(c => c.StudentId == studentId)
                .OrderBy(c => c.Semester!.StartYear).ThenBy(c => c.Semester!.Term)
                .AsNoTracking().ToListAsync();

            var rows = new List<ConductScoreRowViewModel>();
            var counted = new List<int>();

            foreach (var c in list)
            {
                counted.Add(c.Score);
                var row = new ConductScoreRowViewModel
                {
                    RowNumber = rows.Count + 1,
                    ScoreId = c.Id,
                    AcademicYear = c.Semester!.AcademicYear,
                    TermName = c.Semester.TermName,
                    Score = c.Score,
                    Rank = RankHelper.GetRank(c.Score),
                    CumulativeScore = Round(counted.Average()),   // trung bình các kỳ tính đến hiện tại
                    Feedback = c.Feedback,
                    Reply = c.AdminReply,
                    RepliedBy = c.RepliedBy,
                    RepliedAt = c.RepliedAt
                };
                row.CumulativeRank = RankHelper.GetRank(row.CumulativeScore);

                // điểm cả năm: chỉ ở dòng học kỳ 2, khi có đủ học kỳ 1 cùng năm
                if (c.Semester.Term == 2)
                {
                    var term1 = list.FirstOrDefault(x =>
                        x.Semester!.StartYear == c.Semester.StartYear && x.Semester.Term == 1);
                    if (term1 != null)
                    {
                        row.YearScore = Round((term1.Score + c.Score) / 2.0);
                        row.YearRank = RankHelper.GetRank(row.YearScore.Value);
                    }
                }
                rows.Add(row);
            }
            return rows;
        }
    }
}