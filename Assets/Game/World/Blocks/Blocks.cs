using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Blocks
{
    public static Block[] blocks = new Block[18] {

        new Block()
        {
            name = "Air",
            blockId = 0,
            drawBlock = false,
            isSolid = false,
            isLiquid = false,
            isTransparent = true,

            backFaceTexture = 0,
            frontFaceTexture = 0,
            topFaceTexture = 0,
            bottomFaceTexture = 0,
            leftFaceTexture = 0,
            rightFaceTexture = 0,
        },

        new Block()
        {
            name = "Air_Solid",
            blockId = 1,
            drawBlock = true,
            isSolid = true,
            isLiquid = false,
            isTransparent = false,

            backFaceTexture = 0,
            frontFaceTexture = 0,
            topFaceTexture = 0,
            bottomFaceTexture = 0,
            leftFaceTexture = 0,
            rightFaceTexture = 0,
        },

        new Block()
        {
            name = "Grass",
            blockId = 2,
            drawBlock = true,
            isSolid = true,
            isLiquid = false,
            isTransparent = false,

            backFaceTexture = 3,
            frontFaceTexture = 3,
            topFaceTexture = 4,
            bottomFaceTexture = 2,
            leftFaceTexture = 3,
            rightFaceTexture = 3,
        },


        new Block()
        {
            name = "Dirt",
            blockId = 3,
            drawBlock = true,
            isSolid = true,
            isLiquid = false,
            isTransparent = false,

            backFaceTexture = 2,
            frontFaceTexture = 2,
            topFaceTexture = 2,
            bottomFaceTexture = 2,
            leftFaceTexture = 2,
            rightFaceTexture = 2,
        },

        new Block()
        {
            name = "Stone",
            blockId = 4,
            drawBlock = true,
            isSolid = true,
            isLiquid = false,
            isTransparent = false,

            backFaceTexture = 5,
            frontFaceTexture = 5,
            topFaceTexture = 5,
            bottomFaceTexture = 5,
            leftFaceTexture = 5,
            rightFaceTexture = 5,
        },

        new Block()
        {
            name = "OakLog",
            blockId = 5,
            drawBlock = true,
            isSolid = true,
            isLiquid = false,
            isTransparent = false,

            backFaceTexture = 9,
            frontFaceTexture = 9,
            topFaceTexture = 10,
            bottomFaceTexture = 10,
            leftFaceTexture = 9,
            rightFaceTexture = 9,
        },

        new Block()
        {
            name = "OakPlank",
            blockId = 6,
            drawBlock = true,
            isSolid = true,
            isLiquid = false,
            isTransparent = false,

            backFaceTexture = 15,
            frontFaceTexture = 15,
            topFaceTexture = 15,
            bottomFaceTexture = 15,
            leftFaceTexture = 15,
            rightFaceTexture = 15,
        },

        new Block()
        {
            name = "OakPlankMoss",
            blockId = 7,
            drawBlock = true,
            isSolid = true,
            isLiquid = false,
            isTransparent = false,

            backFaceTexture = 16,
            frontFaceTexture = 16,
            topFaceTexture = 16,
            bottomFaceTexture = 16,
            leftFaceTexture = 16,
            rightFaceTexture = 16,
        },

        new Block()
        {
            name = "OakLeaf",
            blockId = 8,
            drawBlock = true,
            isSolid = true,
            isLiquid = false,
            isTransparent = false,

            backFaceTexture = 11,
            frontFaceTexture = 11,
            topFaceTexture = 11,
            bottomFaceTexture = 11,
            leftFaceTexture = 11,
            rightFaceTexture = 11,
        },

        new Block()
        {
            name = "Sand",
            blockId = 9,
            drawBlock = true,
            isSolid = true,
            isLiquid = false,
            isTransparent = false,

            backFaceTexture = 6,
            frontFaceTexture = 6,
            topFaceTexture = 6,
            bottomFaceTexture = 6,
            leftFaceTexture = 6,
            rightFaceTexture = 6,
        },

        new Block()
        {
            name = "Snow",
            blockId = 10,
            drawBlock = true,
            isSolid = true,
            isLiquid = false,
            isTransparent = false,

            backFaceTexture = 8,
            frontFaceTexture = 8,
            topFaceTexture = 7,
            bottomFaceTexture = 2,
            leftFaceTexture = 8,
            rightFaceTexture = 8,
        },

        new Block()
        {
            name = "Water",
            blockId = 11,
            drawBlock = true,
            isSolid = false,
            isLiquid = true,
            isTransparent = false,

            backFaceTexture = 1,
            frontFaceTexture = 1,
            topFaceTexture = 1,
            bottomFaceTexture = 1,
            leftFaceTexture = 1,
            rightFaceTexture = 1,
        },

        new Block()
        {
            name = "StoneBrick",
            blockId = 12,
            drawBlock = true,
            isSolid = true,
            isLiquid = false,
            isTransparent = false,

            backFaceTexture = 13,
            frontFaceTexture = 13,
            topFaceTexture = 13,
            bottomFaceTexture = 13,
            leftFaceTexture = 13,
            rightFaceTexture = 13,
        },

        new Block()
        {
            name = "StoneMossBrick",
            blockId = 13,
            drawBlock = true,
            isSolid = true,
            isLiquid = false,
            isTransparent = false,

            backFaceTexture = 14,
            frontFaceTexture = 14,
            topFaceTexture = 14,
            bottomFaceTexture = 14,
            leftFaceTexture = 14,
            rightFaceTexture = 14,
        },

        new Block()
        {
            name = "Glass",
            blockId = 14,
            drawBlock = true,
            isSolid = true,
            isLiquid = false,
            isTransparent = false,

            backFaceTexture = 12,
            frontFaceTexture = 12,
            topFaceTexture = 12,
            bottomFaceTexture = 12,
            leftFaceTexture = 12,
            rightFaceTexture = 12,
        },

        new Block()
        {
            name = "Dorb",
            blockId = 15,
            drawBlock = true,
            isSolid = true,
            isLiquid = false,
            isTransparent = false,

            backFaceTexture = 17,
            frontFaceTexture = 17,
            topFaceTexture = 17,
            bottomFaceTexture = 17,
            leftFaceTexture = 17,
            rightFaceTexture = 17,
        },

        new Block()
        {
            name = "Straw",
            blockId = 16,
            drawBlock = true,
            isSolid = true,
            isLiquid = false,
            isTransparent = false,

            backFaceTexture = 18,
            frontFaceTexture = 18,
            topFaceTexture = 18,
            bottomFaceTexture = 18,
            leftFaceTexture = 18,
            rightFaceTexture = 18,
        },

        new Block()
        {
            name = "OakPlankSingle",
            blockId = 17,
            drawBlock = true,
            isSolid = true,
            isLiquid = false,
            isTransparent = false,

            backFaceTexture = 19,
            frontFaceTexture = 19,
            topFaceTexture = 19,
            bottomFaceTexture = 19,
            leftFaceTexture = 19,
            rightFaceTexture = 19,
        },
    };
}
