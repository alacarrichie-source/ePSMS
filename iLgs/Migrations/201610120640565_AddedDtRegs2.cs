namespace iLgs.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddedDtRegs2 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "test", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "test");
        }
    }
}
