namespace iLgs.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class modified : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.AspNetUsers", "DtRegs");
            DropColumn("dbo.AspNetUsers", "test");
        }
        
        public override void Down()
        {
            AddColumn("dbo.AspNetUsers", "test", c => c.String());
            AddColumn("dbo.AspNetUsers", "DtRegs", c => c.DateTime(nullable: false));
        }
    }
}
