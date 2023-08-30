using MySqlConnector;
using System.Text;

namespace CourseService.Utils
{
    public class Utils
    {
        public static (string sqlFinal, List<MySqlParameter> parameters) GenerateSql(string[] nameProperties, List<(int id, Dictionary<string, dynamic> data)> items)
        {
            StringBuilder sqlBuilder = new StringBuilder();
            List<MySqlParameter> parameters = new List<MySqlParameter>();

            sqlBuilder.AppendLine("UPDATE courses SET");

            foreach (string propertyName in nameProperties)
            {
                string parameterName = $"@{propertyName}";

                sqlBuilder.AppendLine($"{propertyName} = CASE");

                foreach (var item in items)
                {
                    if (item.data.TryGetValue(propertyName, out dynamic value))
                    {
                        if (value != null)
                        {
                            string valueParameterName = $"{parameterName}_{item.id}";
                            sqlBuilder.AppendLine($" WHEN id = {item.id} THEN {valueParameterName}");
                            parameters.Add(new MySqlParameter(valueParameterName, value));
                        }
                    }
                }
                sqlBuilder.AppendLine($" ELSE {propertyName} END,");
            }

            sqlBuilder.Length -= 3;

            List<int> ids = items.Select(item => item.id).ToList();

            string sqlFooter = $"WHERE courses.Id IN ({string.Join(", ", ids)});";

            string sqlFinal = sqlBuilder.ToString() + " " + sqlFooter;

            return (sqlFinal, parameters);
        }
    }
}