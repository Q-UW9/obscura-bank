-- 0002: cap email length (E1)
-- 254 is the practical maximum for an address
-- this also keeps huge values out of the unique index
-- 0001 is applied and must never be edited, so this is a new file
ALTER TABLE users
    ADD CONSTRAINT users_email_length CHECK (length(email) <= 254);