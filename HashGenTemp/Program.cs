using BCrypt.Net;
class Program {
	static void Main() {
		var hash = BCrypt.Net.BCrypt.HashPassword("missao04091940H");
		Console.WriteLine(hash);
	}
}
