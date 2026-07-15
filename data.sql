--
-- PostgreSQL database dump
--

\restrict 2J7X1s2HK96ztI4ebtD3KnifkSYqN3aq4lJsUGYUJbvQflGQrf1QcbVgbxrUcnE

-- Dumped from database version 18.4
-- Dumped by pg_dump version 18.4

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Data for Name: AspNetRoles; Type: TABLE DATA; Schema: public; Owner: vini
--



--
-- Data for Name: AspNetRoleClaims; Type: TABLE DATA; Schema: public; Owner: vini
--



--
-- Data for Name: AspNetUsers; Type: TABLE DATA; Schema: public; Owner: vini
--

INSERT INTO public."AspNetUsers" VALUES (2, 'Mariah Doe', 'MariahDoe@mail.com', '23456', 0, NULL, false, false, NULL, NULL, NULL, NULL, false, NULL, false, NULL);
INSERT INTO public."AspNetUsers" VALUES (4, 'Juan Donetsky', 'juan@donetsky.com', 'AQAAAAIAAYagAAAAEM1lm1C+adYhxpl96vm3D75ewUD9oGTDLJfY6ozteWsyLHY0CfeErUE3bLNqQQf0ww==', 0, NULL, false, false, NULL, NULL, NULL, NULL, false, NULL, false, NULL);
INSERT INTO public."AspNetUsers" VALUES (5, 'Alan Wake', 'alan@wake.com', 'AQAAAAIAAYagAAAAEIui7fEi4mLrZYQSb7r84KBQ33tWSb87ZjAaeeheQE8H4E8T8FilN94ChP4PCSMY1A==', 0, NULL, false, false, NULL, NULL, NULL, NULL, false, NULL, false, NULL);
INSERT INTO public."AspNetUsers" VALUES (6, 'Fyodr Dostoevsky', 'fiodr@dostoevsky.com', 'AQAAAAIAAYagAAAAEGYHHgowflYPrSYJLnjnfAu7n0G11vB53lt9RDh6n4lz2/V0N9IcuJNhjYIoilLOyg==', 0, 'f7b397fc-1f8c-437d-ba15-d10e7c62d489', false, false, NULL, NULL, NULL, NULL, false, NULL, false, NULL);


--
-- Data for Name: AspNetUserClaims; Type: TABLE DATA; Schema: public; Owner: vini
--



--
-- Data for Name: AspNetUserLogins; Type: TABLE DATA; Schema: public; Owner: vini
--



--
-- Data for Name: AspNetUserRoles; Type: TABLE DATA; Schema: public; Owner: vini
--



--
-- Data for Name: AspNetUserTokens; Type: TABLE DATA; Schema: public; Owner: vini
--



--
-- Data for Name: __EFMigrationsHistory; Type: TABLE DATA; Schema: public; Owner: vini
--

INSERT INTO public."__EFMigrationsHistory" VALUES ('20260630231516_InitialCreate', '9.0.4');
INSERT INTO public."__EFMigrationsHistory" VALUES ('20260708002809_UserModelAlter', '9.0.4');
INSERT INTO public."__EFMigrationsHistory" VALUES ('20260714172735_UserIdentity', '9.0.4');


--
-- Name: AspNetRoleClaims_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: vini
--

SELECT pg_catalog.setval('public."AspNetRoleClaims_Id_seq"', 1, false);


--
-- Name: AspNetRoles_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: vini
--

SELECT pg_catalog.setval('public."AspNetRoles_Id_seq"', 1, false);


--
-- Name: AspNetUserClaims_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: vini
--

SELECT pg_catalog.setval('public."AspNetUserClaims_Id_seq"', 1, false);


--
-- Name: Users_UserID_seq; Type: SEQUENCE SET; Schema: public; Owner: vini
--

SELECT pg_catalog.setval('public."Users_UserID_seq"', 6, true);


--
-- PostgreSQL database dump complete
--

\unrestrict 2J7X1s2HK96ztI4ebtD3KnifkSYqN3aq4lJsUGYUJbvQflGQrf1QcbVgbxrUcnE

