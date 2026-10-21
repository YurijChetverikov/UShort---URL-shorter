--
-- PostgreSQL database dump
--

\restrict 3mj8aEzf3S1tsQRnLczoa6n4FRoMxvxhqiB9X3rqC9ARqgWNaDRe26jC1knSoLZ

-- Dumped from database version 18.6
-- Dumped by pg_dump version 18.6

-- Started on 2026-10-21 17:59:36

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

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- TOC entry 222 (class 1259 OID 16416)
-- Name: events; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.events (
    id integer NOT NULL,
    link_id integer NOT NULL,
    "timestamp" timestamp(3) with time zone NOT NULL,
    continent_geoname_id integer,
    latitude numeric(6,4),
    longitude numeric(6,4),
    accuracy_radius integer,
    country_geoname_id integer,
    city_geoname_id integer,
    event_type "char" NOT NULL,
    user_agent text
);


ALTER TABLE public.events OWNER TO postgres;

--
-- TOC entry 221 (class 1259 OID 16415)
-- Name: events_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.events_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    MAXVALUE 2147483647
    CACHE 1;


ALTER SEQUENCE public.events_id_seq OWNER TO postgres;

--
-- TOC entry 4924 (class 0 OID 0)
-- Dependencies: 221
-- Name: events_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.events_id_seq OWNED BY public.events.id;


--
-- TOC entry 220 (class 1259 OID 16390)
-- Name: links; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.links (
    id integer NOT NULL,
    link text NOT NULL,
    created_at timestamp(3) without time zone CONSTRAINT links_added_not_null NOT NULL
);


ALTER TABLE public.links OWNER TO postgres;

--
-- TOC entry 223 (class 1259 OID 16510)
-- Name: links_hashes; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.links_hashes (
    hash bytea NOT NULL,
    link_id integer NOT NULL
);


ALTER TABLE public.links_hashes OWNER TO postgres;

--
-- TOC entry 219 (class 1259 OID 16389)
-- Name: links_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.links_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    MAXVALUE 2147483647
    CACHE 1;


ALTER SEQUENCE public.links_id_seq OWNER TO postgres;

--
-- TOC entry 4925 (class 0 OID 0)
-- Dependencies: 219
-- Name: links_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.links_id_seq OWNED BY public.links.id;


--
-- TOC entry 4765 (class 2604 OID 16439)
-- Name: events id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.events ALTER COLUMN id SET DEFAULT nextval('public.events_id_seq'::regclass);


--
-- TOC entry 4764 (class 2604 OID 16457)
-- Name: links id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.links ALTER COLUMN id SET DEFAULT nextval('public.links_id_seq'::regclass);


--
-- TOC entry 4769 (class 2606 OID 16441)
-- Name: events events_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.events
    ADD CONSTRAINT events_pkey PRIMARY KEY (id);


--
-- TOC entry 4771 (class 2606 OID 16517)
-- Name: links_hashes links_hashes_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.links_hashes
    ADD CONSTRAINT links_hashes_pkey PRIMARY KEY (hash);


--
-- TOC entry 4767 (class 2606 OID 16459)
-- Name: links links_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.links
    ADD CONSTRAINT links_pkey PRIMARY KEY (id);


-- Completed on 2026-10-21 17:59:37

--
-- PostgreSQL database dump complete
--

\unrestrict 3mj8aEzf3S1tsQRnLczoa6n4FRoMxvxhqiB9X3rqC9ARqgWNaDRe26jC1knSoLZ

