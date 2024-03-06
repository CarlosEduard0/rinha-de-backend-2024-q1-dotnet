drop table if exists "Transactions";
drop table if exists "Clients";

create table "Clients" (
    "Id" int primary key,
    "Limit" int not null,
    "Balance" int not null
);

insert into "Clients" values
    (1, 100000, 0),
    (2, 80000, 0),
    (3, 1000000, 0),
    (4, 10000000, 0),
    (5, 500000, 0);

create unlogged table "Transactions" (
    "Id" serial primary key,
    "Amount" int not null,
    "OperationType" char(1) not null,
    "Description" varchar(10) not null,
    "CreatedAt" timestamp without time zone not null,
    "ClientId" int not null
);
