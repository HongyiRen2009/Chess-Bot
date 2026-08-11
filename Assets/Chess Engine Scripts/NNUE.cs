using EngineCore;
using System;

//public abstract class NNUELayer {
//    public int[] outputs;
//    public abstract int[] Forward(int[] input);
//    public abstract int[] UpdateOutput(int[] input);
//}
//public class LinearLayer:NNUELayer
//{
//    private int[] weights;
//    private int[] biases;
//    private int inFeatures;
//    private int outFeatures;
//    public LinearLayer(int inFeatures, int outFeatures)
//    {
//        outputs = new int[inFeatures];
//        weights = new int[inFeatures*outFeatures];
//        biases = new int[outFeatures];
//        this.inFeatures = inFeatures;
//        this.outFeatures = outFeatures;
//    }
//    public override int[] Forward(int[] input)
//    {
//        Array.Fill(outputs, 0);
//        for (int i = 0; i < inFeatures; i++)
//        {
//            for(int j = 0; j < outFeatures; j++)
//            {
//                outputs[j] += input[i] * weights[i*inFeatures+j];
//            }
//        }
//        for(int i = 0; i < outFeatures; i++)
//        {
//            outputs[i] += biases[i];
//        }
//        return outputs;
//    }
//    public override int[] UpdateOutput(int[] input)
//    {
//       for(int i = 0; i < inFeatures; i++)
//        {
//            if (input[i] == 0) continue;
//            for(int j=0;j<outFeatures; j++)
//            {
//                outputs[j] += input[i] * weights[i * inFeatures + j];
//            }
//        }
//        return outputs;
//    }

//}   
//public class ClippedReluLayer:NNUELayer
//{
//    public ClippedReluLayer(int inFeatures) {
//        outputs = new int[inFeatures];
//    }
//    public override int[] Forward(int[] input)
//    {
//        Array.Fill(outputs, 0);
//        for (int i = 0;i < input.Length; i++)
//        {
//            outputs[i] = Math.Min(Math.Max(input[i], 0), 1);
//        }
//        return outputs;

//    }

//}
public class NNUE
{
    private const int HIDDEN_LAYER_SIZE = 512;
    private const int INPUT_LAYER_SIZE = 64 * 64 * 10;
    private int[] Input = new int[INPUT_LAYER_SIZE];
    private int[] HiddenWeights = new int[INPUT_LAYER_SIZE * HIDDEN_LAYER_SIZE];
    private int[] HiddenBiases = new int[HIDDEN_LAYER_SIZE];
    private int[] FinalWeights = new int[HIDDEN_LAYER_SIZE];
    private int[] HiddenAccumulator = new int[HIDDEN_LAYER_SIZE];
    private int FinalBias = 0;
    private const int L1_SCALE = 255;
    private const int OUTPUT_SCALE = 64;
    private const int SCALE_K = 400;


    private const int DE_SCALE = L1_SCALE* OUTPUT_SCALE;
    private int GetInputIndex(int kingSquare, int pieceSquare, int piece)
    {
        return pieceSquare + (piece + kingSquare * 10) * 64;
    }
    private int CRelu(int x)
    {
        return Math.Clamp(x, 0, 1);
    }
    public void InitializeEvaluation(Board board, bool isWhite) {
        Array.Fill(Input, 0, 0, Input.Length);
        int kingSquare = board.GetKingSquare(isWhite);
        for(int pieceSquare = 0; pieceSquare < 64; pieceSquare++)
        {
            int piece = board.GetPiece(pieceSquare);
            if (piece != Piece.none)
            {
                Input[GetInputIndex(kingSquare, pieceSquare, piece)] = 1;
            }
        }
        for(int i = 0; i < INPUT_LAYER_SIZE; i++)
        {
            for(int j = 0; j < HIDDEN_LAYER_SIZE; j++)
            {
                HiddenAccumulator[j] = Input[i] * HiddenWeights[i * HIDDEN_LAYER_SIZE + j];
            }
        }
        for(int i = 0;i< HIDDEN_LAYER_SIZE; i++)
        {
            HiddenAccumulator[i] += HiddenBiases[i];
        }
    }
    public int GetEvaluation()
    {
        int eval = 0;
        for(int i = 0; i < HIDDEN_LAYER_SIZE; i++)
        {
            eval += CRelu(HiddenAccumulator[i]) * FinalWeights[i];
        }
        eval += FinalBias;

        eval *= SCALE_K;
        eval /= DE_SCALE;
        return eval;
    }
    public void UpdateEvaluation(int kingSquare, int pieceSquare, int piece, bool add)
    {
        int index = GetInputIndex(kingSquare, pieceSquare, piece);
        if (add)
        {
            Input[index] = 1;
            for (int i = 0; i < HIDDEN_LAYER_SIZE; i++)
            {
                HiddenAccumulator[i] += HiddenWeights[index * HIDDEN_LAYER_SIZE + i];
            }
        }
        else
        {
            Input[index] = 0;
            for (int i = 0; i < HIDDEN_LAYER_SIZE; i++)
            {
                HiddenAccumulator[i] -= HiddenWeights[index * HIDDEN_LAYER_SIZE + i];
            }
        }


    }
}
