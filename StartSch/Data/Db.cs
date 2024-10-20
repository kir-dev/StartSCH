using Microsoft.EntityFrameworkCore;

namespace StartSch.Data;

public class Db(DbContextOptions<Db> options) : DbContext(options)
{
}